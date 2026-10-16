# 🔍 LoanLens — محرك الاستشارات المالية وتحليل القروض الذكي

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](#architecture)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ed.svg)](#docker)

**LoanLens** هو محرك مالي وتحليلي متكامل (Financial Advisory & Decisioning Engine) مصمم لمساعدة الأفراد والجهات التمويلية في اتخاذ قرارات الاقتراض الرشيدة، مع مراعاة الضوابط التنظيمية للبنك المركزي المصري (CBE) وتقلبات السوق.

---

## ✨ المميزات الرئيسية (Core Features)

1. **حساب جداول السداد (Amortization Schedule):**
   - دعم الفائدة المتناقصة (Reducing Balance) والفائدة الثابتة (Flat Rate).
   - التحويل الدقيق لمعدل الفائدة الفعلي السنوي (APR / EAR).
   - حساب المصاريف الإدارية والدمغات النسبية.

2. **تحليل القدرة على تحمل الدين (Affordability & DTI):**
   - فحص نسبة عبء الدين (Debt-To-Income - DTI) وفقاً لسقف البنك المركزي المصري (50% كحد أقصى، و35% كحد آمن موصى به).
   - حساب الدخل المتبقي القابل للتصرف (Disposable Income) ومؤشر مدرج الطوارئ (Emergency Runway Months).

3. **اختبارات التحمل المالي (Deterministic Stress Testing):**
   - محاكاة سيناريوهات الأزمات: فقدان الوظيفة (شهر / 3 شهور / 6 شهور)، نقص الدخل بنسبة 25%، الطوارئ الطبية، وصدمات التضخم.

4. **محاكاة مونت كارلو الاحتمالية (Monte Carlo Stochastic Simulation):**
   - محاكاة 5,000+ مسار عشوائي لاحتمالية التعثر (Default Probability %)، وتحديد شهر ذروة المخاطر، وتوزيع المدخرات عند المئينات (5th, 50th, 95th).

5. **تقييم المخاطر والقرار النهائي (0-100 Risk Scoring & Decision Engine):**
   - تقييم مركب للمخاطر (DTI, Emergency Buffer, Expense Ratio, Income Stability).
   - قرار استشاري نهائي واضح: قبول القرض (Take Loan)، حذر مشروط (Proceed with Caution)، أو تجنب القرض (Avoid Loan).

6. **دمج المديونيات وإعادة الهيكلة (Debt Buyout & Consolidation):**
   - تقييم شراء المديونيات المتعددة (قروض وبطاقات ائتمان) وحساب غرامات السداد المعجل والوفر في السيولة الشهرية.

7. **حساسية تغير أسعار الفائدة (CBE Corridor Sensitivity):**
   - محاكاة تأثير تحريك أسعار الكوريدور (+100, +200, +300 نقطة أساس) على القروض متغيرة الفائدة.

8. **تصدير التقارير (PDF & Excel Reports):**
   - توليد تقرير PDF شامل وتفاعلي عبر مكتبة `QuestPDF`.
   - تصدير جدول الاستهلاك الكامل بصيغة Microsoft Excel (`.xlsx`) عبر مكتبة `ClosedXML`.

9. **المستشار الذكي (AI Financial Advisor):**
   - صياغة تقرير سردي ذكي وشخصي بالكامل باللغة العربية أو الإنجليزية باستخدام نماذج Google Gemini أو Groq.
   - دعم تلقائي لكشف لغة العميل واستفساراته (Auto-detect Language).

---

## 🏛️ البنية المعمارية (Architecture)

تم بناء المشروع باتباع أحدث مبادئ **Clean Architecture** لضمان فصل الاهتمامات وقابلية التوسع والاختبار:

```
LoanLens/
├── src/
│   ├── LoanLens.Domain/             # النواة: الكيانات، الـ Value Objects، والـ Enums الخالصة
│   │   ├── Enums/                   # ملف مستقل لكل Enum (Single Type Per File)
│   │   ├── ValueObjects/            # نماذج الحسابات المالية والنتائج
│   │   ├── Services/                # محركات الحسابات المالية (Engines)
│   │   └── Localization/            # دعم اللغتين العربية والإنجليزية
│   │
│   ├── LoanLens.Application/        # طبقة التطبيق: الـ DTOs والواجهات (Interfaces)
│   │   ├── DTOs/                    # ملف مستقل لكل DTO
│   │   ├── Interfaces/              # عقود الخدمات ومستودعات البيانات
│   │   └── Services/                # LoanAdvisorService لتنسيق مسار العمليات
│   │
│   ├── LoanLens.Infrastructure/     # طبقة البنية التحتية: التنفيذ الفعلي
│   │   ├── Persistence/             # EF Core DbContext والـ Migrations (SQLite)
│   │   ├── Repositories/            # تنفيذ حفظ واسترجاع السيناريوهات
│   │   └── Services/                # تنفيذات تصدير PDF و Excel و AI Advisor
│   │
│   └── LoanLens.Api/                # طبقة العرض والـ Endpoints (Controllers)
│       ├── Controllers/             # RESTful API Controllers
│       ├── Middleware/              # CorrelationId و Error Handling و Rate Limiting
│       └── Health/                  # Health Checks للـ Database و Readiness
│
└── tests/
    └── LoanLens.Domain.Tests/       # 34 اختبار وحدة لاختبار المحركات الرياضية والمالية
```

---

## 🚀 التشغيل السريع (Quickstart)

### المتطلبات المسبقة:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### 1. تشغيل المشروع محلياً:
```bash
# الانتقال لمسار المشروع
cd loanlens

# تثبيت الحزم والبناء
dotnet restore
dotnet build

# تشغيل الاختبارات للتأكد من سلامة الكود
dotnet test tests/LoanLens.Domain.Tests

# تشغيل الـ API
dotnet run --project src/LoanLens.Api
```

بعد التشغيل، افتح المتصفح على:
* **واجهة التوثيق التفاعلية (Scalar):** `http://localhost:5257/scalar/v1`
* **فحص صحة السيرفر (Health Check):** `http://localhost:5257/healthz`

---

## 🔐 إعداد مفاتيح الذكاء الاصطناعي (AI Keys Configuration)

للحفاظ على أمان المفاتيح وعدم رفعها على Git، يتم استخدام **.NET User Secrets** في بيئة التطوير:

```bash
# إعداد مفتاح Google Gemini
dotnet user-secrets set "AiAdvisor:GeminiApiKey" "YOUR_GEMINI_KEY" --project src/LoanLens.Api

# إعداد مفتاح Groq
dotnet user-secrets set "AiAdvisor:GroqApiKey" "YOUR_GROQ_KEY" --project src/LoanLens.Api
```

أو في بيئة الإنتاج كمتغيرات بيئية (Environment Variables):
```bash
export AiAdvisor__GeminiApiKey="YOUR_KEY"
export AiAdvisor__GroqApiKey="YOUR_KEY"
```

---

## 🐳 التشغيل باستخدام Docker

### تشغيل الحاوية باستخدام Docker Compose:
```bash
docker compose up -d --build
```

سيتم تشغيل السيرفر على المنفذ `5257` مع ربط قاعدة بيانات SQLite بمجلد تخزين دائم (`loanlens-data`).

---

## 📡 قائمة الـ API Endpoints

| Method | Endpoint | Description |
|:---|:---|:---|
| `POST` | `/api/v1/loans/calculate` | حساب جدول السداد والـ APR والرسوم |
| `POST` | `/api/v1/loans/affordability` | تحليل عبء الدين DTI ومدرج الطوارئ |
| `POST` | `/api/v1/loans/stress-test` | اختبارات الضغط المالي الحتمية |
| `POST` | `/api/v1/loans/monte-carlo` | محاكاة مونت كارلو لاحتمالية التعثر |
| `POST` | `/api/v1/loans/full-analysis` | تقرير شامل 360° متضمن كافة المحركات |
| `GET`  | `/api/v1/loans/catalog` | كتالوج القروض وبطاقات الائتمان في مصر |
| `POST` | `/api/v1/loans/export-pdf` | تصدير تقرير تحليلي مصمم بصيغة PDF |
| `POST` | `/api/v1/loans/export-excel` | تصدير جدول الأقساط إلى ملف Excel (.xlsx) |
| `POST` | `/api/v1/loans/save-scenario` | حفظ السيناريو في قاعدة البيانات |
| `GET`  | `/api/v1/loans/scenarios` | عرض السيناريوهات المحفوظة مسبقاً |
| `GET`  | `/api/v1/loans/scenarios/{id}` | استرجاع سيناريو محدد بالمعرف |
| `GET`  | `/api/v1/loans/scenarios/{id}/pdf` | تحميل تقرير PDF للسيناريو المحفوظ |
| `GET`  | `/api/v1/loans/scenarios/{id}/excel` | تحميل ملف Excel للسيناريو المحفوظ |
| `POST` | `/api/v1/loans/compare` | مقارنة سيناريوهين مختلفين جنباً إلى جنب |
| `GET`  | `/api/v1/loans/scenarios/compare` | مقارنة سيناريوهين محفوظين بواسطة الـ IDs |
| `POST` | `/api/v1/loans/borrowing-capacity` | حساب الحد الأقصى للاقتراض المسموح به |
| `POST` | `/api/v1/loans/debt-consolidation` | محاكاة وتدقيق دمج وشراء المديونيات |
| `POST` | `/api/v1/loans/rate-sensitivity` | تحليل حساسية الفائدة لقرارات البنك المركزي |
| `POST` | `/api/v1/loans/ai-advice` | صياغة استشارة مالية ذكية ومخصصة بالذكاء الاصطناعي |
| `GET`  | `/healthz` & `/ready` | مؤشرات الجاهزية وصحة النظام وقاعدة البيانات |

---

## 🧪 الاختبارات الآلية (Testing)

لتشغيل الـ 34 اختبار وحدة والتأكد من دقة العمليات الحسابية:
```bash
dotnet test tests/LoanLens.Domain.Tests
```
