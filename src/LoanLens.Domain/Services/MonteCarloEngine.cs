using System.Diagnostics;
using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class MonteCarloEngine : IMonteCarloEngine
{
    public MonteCarloSimulationResult RunSimulation(
        FinancialProfile profile, 
        LoanParameters loan, 
        decimal monthlyInstallment, 
        int iterations = 5000)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(loan);
        if (iterations <= 0) iterations = 5000;

        var stopwatch = Stopwatch.StartNew();

        int tenor = loan.TenorMonths;
        decimal baseIncome = profile.TotalNetIncome;
        decimal baseFixedExpenses = profile.FixedMonthlyExpenses;
        decimal otherInstallments = profile.CurrentMonthlyInstallments;
        decimal initialSavings = profile.Savings;

        // Volatility depending on income stability
        double incomeVolatilitySigma = profile.Stability switch
        {
            IncomeStability.StableEmployed => 0.04,  // 4% variance
            IncomeStability.Freelance => 0.18,       // 18% variance
            IncomeStability.BusinessOwner => 0.22,   // 22% variance
            _ => 0.08
        };

        // Random emergency frequencies
        double minorShockProbability = 0.08; // 8% monthly chance
        double majorShockProbability = 0.015; // 1.5% monthly chance

        int defaultCount = 0;
        var defaultMonthsCounter = new int[tenor + 1];
        long totalDefaultMonthSum = 0;

        var endingSavingsArray = new decimal[iterations];

        // Parallel execution across CPU cores for maximum speed
        Parallel.For(0, iterations, () => (DefaultsCount: 0, LocalDefaultsPerMonth: new int[tenor + 1], DefaultSum: 0L),
            (i, loopState, localState) =>
            {
                var rand = Random.Shared;
                decimal currentSavings = initialSavings;
                bool hasDefaulted = false;
                int defaultedAtMonth = 0;

                for (int m = 1; m <= tenor; m++)
                {
                    // Gaussian shock for income using Box-Muller
                    double u1 = Math.Max(1e-9, rand.NextDouble());
                    double u2 = rand.NextDouble();
                    double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

                    double incomeMultiplier = 1.0 + (z * incomeVolatilitySigma);
                    if (incomeMultiplier < 0.2) incomeMultiplier = 0.2; // Don't go completely negative from noise alone

                    decimal simulatedIncome = Math.Round(baseIncome * (decimal)incomeMultiplier, 2);
                    decimal totalOutflow = baseFixedExpenses + otherInstallments + monthlyInstallment;

                    // Random minor emergency shock
                    if (rand.NextDouble() < minorShockProbability)
                    {
                        totalOutflow += (decimal)(rand.Next(2000, 8000));
                    }

                    // Random major disruption (medical, car breakdown, job gap)
                    if (rand.NextDouble() < majorShockProbability)
                    {
                        if (profile.HasHealthInsurance)
                            totalOutflow += (decimal)(rand.Next(5000, 15000));
                        else
                            totalOutflow += (decimal)(rand.Next(15000, 45000));
                    }

                    decimal netCashflow = simulatedIncome - totalOutflow;
                    currentSavings += netCashflow;

                    // If savings drop below zero AND current monthly cashflow cannot cover installment -> Default
                    if (!hasDefaulted && currentSavings < 0 && netCashflow < 0)
                    {
                        hasDefaulted = true;
                        defaultedAtMonth = m;
                    }
                }

                if (hasDefaulted)
                {
                    localState.DefaultsCount++;
                    localState.DefaultSum += defaultedAtMonth;
                    localState.LocalDefaultsPerMonth[defaultedAtMonth]++;
                }

                endingSavingsArray[i] = currentSavings;

                return localState;
            },
            localState =>
            {
                Interlocked.Add(ref defaultCount, localState.DefaultsCount);
                Interlocked.Add(ref totalDefaultMonthSum, localState.DefaultSum);
                lock (defaultMonthsCounter)
                {
                    for (int m = 1; m <= tenor; m++)
                    {
                        defaultMonthsCounter[m] += localState.LocalDefaultsPerMonth[m];
                    }
                }
            });

        stopwatch.Stop();

        // Calculate statistics
        Array.Sort(endingSavingsArray);

        decimal defaultProbability = Math.Round(((decimal)defaultCount / iterations) * 100m, 2);
        
        int? peakMonth = null;
        int maxDefaultsInMonth = 0;
        var distributionDict = new Dictionary<int, int>(tenor);

        for (int m = 1; m <= tenor; m++)
        {
            var count = defaultMonthsCounter[m];
            distributionDict[m] = count;
            if (count > maxDefaultsInMonth && count > 0)
            {
                maxDefaultsInMonth = count;
                peakMonth = m;
            }
        }

        double avgDefaultMonth = defaultCount > 0 
            ? Math.Round((double)totalDefaultMonthSum / defaultCount, 1) 
            : 0.0;

        int idx5 = (int)(iterations * 0.05);
        int idx50 = (int)(iterations * 0.50);
        int idx95 = (int)(iterations * 0.95);

        decimal worstCaseSavings = Math.Round(endingSavingsArray[Math.Clamp(idx5, 0, iterations - 1)], 2);
        decimal medianSavings = Math.Round(endingSavingsArray[Math.Clamp(idx50, 0, iterations - 1)], 2);
        decimal bestCaseSavings = Math.Round(endingSavingsArray[Math.Clamp(idx95, 0, iterations - 1)], 2);

        return new MonteCarloSimulationResult(
            TotalIterations: iterations,
            DefaultCount: defaultCount,
            DefaultProbabilityPercentage: defaultProbability,
            PeakRiskMonth: peakMonth,
            AverageDefaultMonth: avgDefaultMonth,
            MedianEndingSavings: medianSavings,
            WorstCase5thPercentileSavings: worstCaseSavings,
            BestCase95thPercentileSavings: bestCaseSavings,
            DefaultsByMonthDistribution: distributionDict,
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds
        );
    }
}
