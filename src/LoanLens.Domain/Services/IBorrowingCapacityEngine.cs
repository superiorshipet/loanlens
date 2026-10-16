using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IBorrowingCapacityEngine
{
    BorrowingCapacityResult CalculateCapacity(
        FinancialProfile profile,
        decimal targetInterestRate,
        int targetTenorMonths,
        InterestCalculationMethod calculationMethod = InterestCalculationMethod.Reducing,
        string language = "en");
}
