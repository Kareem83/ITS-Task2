# ITS Task — Full Stack

الواجهة React + TypeScript متصلة بالـ API، وتشمل واجهات Administrator وManager وEmployee، إدارة الموظفين والأقسام وأنواع الإجازات، البحث والفرز والتصفية والصفحات، الأرصدة، طلب الإجازة وإلغائها، الموافقة والرفض، الملف الشخصي وسجل التغييرات. التصميم يدعم الموبايل.

## تشغيل المشروع كاملًا على اللاب

ثبت Docker Desktop وشغله، ثم نزّل الريبو بواسطة Code → Download ZIP وفك الضغط. من مجلد المشروع انسخ `.env.example` إلى `.env`، واضبط SQL_PASSWORD بكلمة قوية وJWT_KEY بقيمة عشوائية لا تقل عن 32 حرفًا. ADMIN_PASSWORD هي كلمة مرور المسؤول عند إنشاء القاعدة أول مرة.

```text
docker compose up --build -d
docker compose ps
```

افتح المتصفح على المنفذ 8080 لجهازك. الواجهة والـ API يعملان من نفس التطبيق، فلا تحتاج تعديل عنوان الربط. أول تشغيل ينشئ قاعدة التطوير المنفصلة ITS_FullStack_Development ويطبق migrations ويضيف حسابات التجربة. SQL Server يحفظ البيانات في volume دائم، وإعادة التشغيل لا تعيد ضبط الأرصدة أو كلمات المرور. التشغيل التالي يستخدم `docker compose up -d`، والإيقاف `docker compose stop`.

| الحساب | البريد | كلمة المرور الافتراضية |
| --- | --- | --- |
| Administrator | admin@its.com | قيمة ADMIN_PASSWORD في .env |
| Manager | manager@its.com | Demo@123 |
| Employee | employee@its.com | Demo@123 |

هذا إعداد تطوير وتجربة. للإنتاج استخدم SQL Server مُدارًا وأسرارًا مستقلة، ASPNETCORE_ENVIRONMENT=Production، Seed__IncludeDemo=false، Database__Initialize=false، وHTTPS على reverse proxy. طبّق migrations بعد مراجعتها. لا تستخدم حسابات التجربة في الإنتاج. لا تحذف volume أو قاعدة موجودة للحصول على تشغيل جديد.

## تطوير الواجهة والباك اند محليًا

تحتاج .NET SDK 8 وNode.js 22.12 أو أحدث وSQL Server. إعداد Windows LocalDB الافتراضي متاح للباك اند. راجع أقسام الاتصال والـ migrations والـ seed أدناه قبل تجهيز قاعدة بيانات قائمة. Database:Initialize افتراضيًا false، ولا يُفعّل إلا عمدًا لقاعدة تطوير جديدة.

من مجلد المشروع شغّل:

```text
dotnet run --project "ITS Task/ITS Task.csproj" --launch-profile http
```

ومن terminal آخر:

```text
cd frontend
npm ci
npm run dev
```

Vite يستخدم المنفذ 5173 ويمرر `/api` للباك اند على 5019. يمكن تغيير API_PROXY_TARGET أو VITE_API_URL كما هو موضح في frontend/.env.example. المتغيرات VITE عامة في المتصفح؛ لا تضع فيها أسرارًا. تسجيل الدخول يستخدم JWT وrefresh token مع حفظ الجلسة في sessionStorage وإنهائها عند تسجيل الخروج أو رفض التجديد. السيرفر يفرض جميع الصلاحيات.

## اختبارات الواجهة والتكامل الكامل

```text
cd frontend
npm test
npm run build
npx playwright install chromium
npm run test:e2e
```

اختبارات Playwright تحتاج الواجهة والباك اند وقاعدة **تطوير منفصلة** شغالة وحسابات seed المذكورة. تنشئ سجلات اختبار جديدة ولا تحذف بيانات. يمكن ضبط E2E_BASE_URL لاختبار الواجهة المجمعة داخل Docker، وCHROMIUM_PATH لاستخدام Chromium مثبت. اختبارات المتصفح تغطي إنشاء قسم وموظف وتعيين مدير، الصلاحيات، تقديم الطلب واعتماده وتحقق الرصيد، الإلغاء والرفض، تعديل وتعطيل نوع الإجازة وعرض الموبايل. التفاصيل والأدلة في Evidence/fullstack-validation.md.

المشروع ASP.NET Core Web API على .NET 8، مع EF Core 8.0.19 وSQL Server وIdentity وJWT. تم الحفاظ على الطبقات والأسماء الأساسية، بما فيها AssignDepartementToEmployee.

## المحتويات

- ITS Task.sln: الحل الذي يعمل مع .NET 8 وVisual Studio.
- ITS Task: التطبيق والـ DTOs والـ Services والـ Repositories والـ Models والـ Middleware والـ Migrations.
- ITS.Task.Tests: Unit tests واختبارات HTTP/خدمات على SQLite منفصل في الذاكرة، واختبارات SQL Server اختيارية.
- database-upgrade.sql: سكربت migrations كامل idempotent للمراجعة والتطبيق اليدوي.
- Evidence: نتائج التحقق.

لا توجد تعليقات في ملفات C# المسلَّمة، ولا ملفات bin/obj/.vs أو بيانات اعتماد حقيقية في الأرشيف.

## البناء والاختبارات

من مجلد الحل:

```text
dotnet restore "ITS Task.sln" --locked-mode
dotnet build "ITS Task.sln" --no-restore
dotnet test "ITS Task.sln" --no-restore
dotnet test "ITS Task.sln" --filter "Category=Unit"
dotnet tool restore
```

تحتاج SDK .NET 8. ملف global.json يسمح بإصدارات feature bands الحديثة من .NET 8. الحزم وتبعياتها مثبتة في packages.lock.json، وdotnet-ef مثبت على 8.0.19 في tool manifest.

## إعداد الاتصال

الإعداد الافتراضي يحافظ على اتصال Windows LocalDB المرفق، ولا يعمل LocalDB على Linux. يمكن تجاوز الاتصال بواسطة ConnectionStrings__DefaultConnection أو User Secrets.

من مجلد الحل، مثال Windows development:

```text
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=ITS_Task_Development;Trusted_Connection=True;TrustServerCertificate=True" --project "ITS Task/ITS Task.csproj"
```

لـ SQL Server آخر استخدم connection string الخاص ببيئة التطوير. يجب استخدام اتصال مشفر وشهادة موثوقة في الإنتاج؛ TrustServerCertificate مناسب فقط لسيرفر التطوير المحلي ذي الشهادة الذاتية.

مفتاح JWT الموجود development-only. ضع مفتاحًا عشوائيًا سريًا لا يقل عن 32 بايت عبر Jwt__Key أو User Secrets للإنتاج، واضبط Jwt:Issuer وJwt:Audience. لا تضع أسرار الإنتاج في الملفات المتتبعة.

## قاعدة البيانات والـ migration الجديدة

لم يتم الاتصال بقاعدة بيانات المستخدم أو تعديلها أثناء التنفيذ. لم تُحذف بيانات، ولم تُنفذ Drop-Database. منطق migrations الثلاث القديمة، وخاصة AddRefreshToken، محفوظ؛ أُزيلت التعليقات فقط.

توجد migration إضافية:

`20261009120000_RepairEmployeeAccountRelationship`

سببها أن migrations القديمة لم تنشئ FK/unique index لعلاقة Employee.UserId رغم وجودهما في model/snapshot. التحديث:

- يبقي UserId اختياريًا للموظفين القدامى، ويحوّل القيم الفارغة إلى NULL.
- يتحقق من الروابط الطويلة والمكررة وغير الصالحة، ويتوقف بخطأ واضح عند وجودها بدل حذف موظفين أو اختيار حساب عشوائي.
- يغيّر UserId إلى nvarchar(450)، ويضيف unique filtered index وFK مع Restrict.
- يضيف IsAnnual وعلامة فريدة لنوع الإجازة السنوية، حتى لا يتعطل Dashboard عند تغيير اسمه. النوع القديم المسمى Annual Leave يُعلَّم تلقائيًا.
- ينشئ الأرصدة الناقصة فقط، دون تعديل الأرصدة الموجودة. Used/Pending للرصيد الجديد محسوبان من طلبات الموظف التاريخية؛ تخصيصه لا يقل عن مجموعهما ولا عن AllowedDays حتى لا ينتج رصيد متبقٍ سالب عند ترميم البيانات القديمة.

قبل تطبيقها على قاعدة موجودة: خذ backup، راجع schema الفعلي و__EFMigrationsHistory وسكربت database-upgrade.sql، وحل أي روابط غير صالحة يُبلغ عنها التحديث. إذا كانت القيود قد أُنشئت يدويًا خارج migrations، راجع ذلك قبل التطبيق. لا تُعدّل migrations القديمة أو تسجل migration كتطبيق ناجح يدويًا لتجاوز المشكلة.

لتطبيق التحديث بعد المراجعة على قاعدة development المختارة:

```text
dotnet ef database update --project "ITS Task/ITS Task.csproj"
```

ولإعادة توليد السكربت دون تطبيقه:

```text
dotnet ef migrations script --idempotent --project "ITS Task/ITS Task.csproj" --output database-upgrade.sql
```

التطبيق لا ينفذ Migrate/EnsureCreated تلقائيًا. الموظف القديم غير المرتبط بحساب يمكن إنشاء حساب له عن طريق POST /api/Employees/{id}/account، بعد تهيئة الأدوار. إذا كان حساب البريد موجودًا بالفعل، endpoint يرفض إنشاء حساب آخر ويطلب حل الربط القائم بدل تخمينه.

## Seed والتشغيل

Seed:Enabled وSeed:IncludeDemo افتراضيًا false، فلا تحدث كتابة seed عند التشغيل العادي.

لتهيئة roles وAdministrator على قاعدة development بعد migrations، مثال PowerShell:

```powershell
$env:Seed__Enabled = "true"
$env:Seed__IncludeDemo = "false"
dotnet run --project "ITS Task/ITS Task.csproj" --launch-profile http
```

لتجربة كاملة على قاعدة development جديدة فارغة، غيّر Seed__IncludeDemo إلى true. هذا ينشئ Engineering وAnnual Leave وSick Leave ومديرًا وموظفًا ورصيدين لكل منهما. يُسمح به في Development فقط. تكراره على بيانات demo القائمة لا يكرر الصفوف، ولا يعيد كلمات السر أو يصفر الأرصدة. يرفض إدخال demo إلى قاعدة تطبيق غير فارغة لا تحتوي بيانات demo المعروفة.

بيانات development الموجودة في appsettings.Development.json:

| الحساب | كلمة السر | الدور |
|---|---|---|
| admin@its.com | Admin@123 | Administrator |
| manager@its.com | Demo@123 | Manager |
| employee@its.com | Demo@123 | Employee |

يمكن تجاوز Seed:AdminPassword وSeed:DemoPassword بأسرار development الخاصة بك. Seed لا يعيد تعيين كلمة سر حساب موجود. لا تستخدم هذه الحسابات أو المفاتيح الافتراضية في الإنتاج.

بعد التهيئة يمكن تعطيل Seed__Enabled وSeed__IncludeDemo. Swagger متاح في Development على /swagger. مثال HTTP موجود في ITS Task.http لتسجيل الدخول.

## الـ API

POST /api/Auth/login يرجع accessToken وrefreshToken. POST /api/Auth/refresh-token يقبل refreshToken ويستبدله بآخر. ضع AccessToken في Bearer header. الحساب المعطل ممنوع من Login وRefresh واستخدام AccessToken الحالي. تغيير الدور يبطل refresh ويرفض AccessToken ذي الأدوار القديمة.

| المجال | المسارات الأساسية |
|---|---|
| Employee profile | GET /api/Employees/me، للموظف والمدير حسب هويتهما |
| Employees | GET/POST /api/Employees، GET/PUT/DELETE /api/Employees/{id} |
| Assignments | PUT /api/Employees/{id}/department?DepartmentId=، /manager?managerId=، /role?role= |
| Clear manager | PUT /api/Employees/{id}/manager?managerId=0 |
| Legacy account | POST /api/Employees/{id}/account مع password |
| Team | GET /api/Employees/my-team و/my-team/search |
| Departments | GET/POST /api/Departments، GET/PUT/DELETE /api/Departments/{id} |
| Leave Types | GET/POST /api/LeaveTypes، GET/PUT /api/LeaveTypes/{id}، PUT /{id}/deactivate |
| Leave Requests | POST /api/LeaveRequests، GET /my و/my-balance و/pending و/team، GET القائمة العامة |
| Leave decisions | PUT /api/LeaveRequests/{id}/approve و/reject و/cancel |
| Dashboards | GET /api/Dashboard/employee و/manager و/admin |
| Audit | GET /api/AuditLogs، Administrator فقط |

إدارة Employees/Departments/LeaveTypes/roles والقائمة العامة للإجازات خاصة بـ Administrator. الموافقة والرفض خاصة بـ Manager لفريقه فقط. التقديم والإلغاء خاصة بـ Employee لطلباته فقط. المدير يمكنه عرض ملفه الشخصي، لكن الأدوار تظل منفصلة كما في المشروع الأصلي.

حذف Employee يعني Deactivation ولا يحذف الإجازات أو الأرصدة أو Audit. لا يمكن حذف Department له أي موظف، حتى لو كان غير نشط. يجب نقل فريق المدير قبل تغيير دوره إلى دور آخر.

## القوائم والعقود

القوائم تدعم page وpageSize بين 1 و100 وsearch وsortBy وsortOrder. page بين 1 و1000000. القيم غير الصحيحة ترجع 400. الفرز ثابت بواسطة Id كمفتاح ثانوي.

Employees تدعم DepartmentId/ManagerId/IsActive/Role، والفرز id/name/hiredate/department. Search يفحص الاسم والبريد والمسمى الوظيفي.

Leave Requests تدعم EmployeeId/DepartmentId/LeaveTypeId/Status/FromDate/ToDate، والفرز id/employee/leavetype/startdate/status/createdat. Search يفحص الموظف وبريده ونوع الإجازة والسبب. نطاق المستخدم/الفريق مفروض في السيرفر ولا يمكن تجاوزه بإضافة EmployeeId.

تغييرات العقود الضرورية للـ pagination:

- /api/Employees/search و/my-team/search يرجعان EmployeePagedResultDto مثل قائمة الموظفين، بدل array غير محدود.
- /api/LeaveRequests/my و/team و/pending يرجعن items/page/pageSize/totalCount/totalPages بدل array غير محدود.
- قائمة إجازات Administrator تحتفظ باسم requests وmetadata الحالية.
- الردود تحتوي IsActive/ManagerId للموظف وIsAnnual لنوع الإجازة كحقول إضافية.

DTOs التقديم/التعديل الحالية باقية. isAnnual اختياري في CreateLeaveTypeDto، وnullable في UpdateLeaveTypeDto؛ عدم إرساله في التعديل يحافظ على العلامة الحالية. إنشاء نوع باسم Annual Leave يعلّمه سنويًا تلقائيًا. لا يُسمح بنوعين سنويين. يمكن ضبط LeaveManagement:AnnualLeaveTypeId لتجاوز اختيار Dashboard عند الحاجة.

## سياسة الرصيد والـ Dashboard

الرصيد المتاح = AllocatedDays - UsedDays - PendingDays. Submit يحجز Pending، Approve ينقله إلى Used، Reject/Cancel يحرران Pending. لا توجد إجازات متداخلة Pending أو Approved للموظف نفسه حتى عبر أنواع مختلفة.

Friday/Saturday مستبعدان. التواريخ يومية وشاملة يوم البداية والنهاية. لا يُفرض منع الإجازات الماضية أو holidays أو reset سنوي لأن ملف task لا ينص عليها.

الموظف الجديد يحصل على رصيد لكل نوع نشط، باستخدام AllowedDays. النوع الجديد ينشئ رصيدًا لكل موظف. تعديل AllowedDays يحدث تخصيص الأرصدة القائمة دون تصفير Used/Pending، ويرفض خفضه إلى أقل من المستخدم والمحجوز. الرصيد لا يتجدد تلقائيًا عند تغيير السنة.

ApprovedThisYear/ApprovedThisMonth يعني عدد طلبات الإجازة المعتمدة التي تبدأ في السنة/الشهر، وهو السلوك الحالي الذي أبقيناه لأن المستند لم يحدد تاريخ قرار الموافقة. EmployeesCurrentlyAway يشمل اليوم الأخير. تحديد النوع السنوي ثابت ولا يعتمد على الاسم بعد التحديث.

## التزامن والأخطاء

كل تعديل للموظف/الدور/نوع الإجازة/القسم، وكل Submit/Approve/Reject/Cancel، يعمل داخل Serializable transaction باستخدام execution strategy الخاص بـ SQL Server. تشمل المعاملة عمليات Identity والرصيد والطلب وAudit حيث يلزم. عند retry تُقرأ البيانات مجددًا بعد تنظيف ChangeTracker.

فحص الرصيد والتداخل وانتقال الحالة داخل المعاملة نفسها؛ SQL Server يحافظ على locks/key-range locks حتى commit. إذا حدث deadlock يُعاد تنفيذ المعاملة وفق EnableRetryOnFailure. العمليات التي تفقد السباق ترى الحالة الجديدة أو الرصيد المحجوز، وترجع 409 بدل مضاعفة الخصم. فشل Audit يرجع المعاملة كلها. لا يوجد clamp صامت للأرصدة السالبة.

Middleware يرجع 400 للمدخلات، 401 للمصادقة، 403 للصلاحيات، 404 لعدم الوجود، 409 للتعارض، و500 للأخطاء الداخلية مع تسجيلها دون عرض تفاصيلها الحساسة.

## اختبارات SQL Server الاختيارية

ضبط ITS_TEST_SQLSERVER على اتصال SQL Server development يسمح باختبارات حقيقية للمigrations والسباقات:

```text
dotnet test "ITS Task.sln" --filter "Category=SqlServer"
```

هذه الاختبارات تغيّر InitialCatalog إلى ITS_Automated_GUID جديد لكل اختبار، وتُطبق migrations والـ seed هناك؛ لا تستخدم قاعدة التطبيق الموجودة ولا تحذف أي قاعدة. يتطلب الحساب صلاحية إنشاء قواعد development، وتبقى القواعد الناتجة متاحة للفحص بعد الاختبار.

إذا لم يكن الاتصال مضبوطًا، الاختبارات الثلاثة تُسجَّل Skipped صراحة. اختبارات SQLite لا تثبت سلوك locks أو migration SQL الخاصة بـ SQL Server. في التحقق الحالي نُفذت الاختبارات الثلاثة على SQL Server 2022 ونجحت دون تخطي.
