#  RestaurantBooking

რესტორნის მაგიდების დაჯავშნის ვებ-აპლიკაცია, რომელიც ASP.NET Core Razor Pages-ზეა აწყობილი და Clean Architecture-ის პრინციპებს მიჰყვება. მომხმარებელი რეგისტრირდება, ჯავშნის მაგიდას სასურველ დროზე და ელფოსტით იღებს დადასტურებას. ადმინისტრატორი ხედავს ყველა მომავალ ჯავშანს და საჭიროების შემთხვევაში აუქმებს მათ.



##  ფუნქციონალი

**მომხმარებლისთვის**
- რეგისტრაცია და შესვლა (JWT ტოკენი ინახება HttpOnly ქუქიში)
- მაგიდის დაჯავშნა: მაგიდის, თარიღის, დროისა და სტუმრების რაოდენობის არჩევა
- საკუთარი ჯავშნების სიის ნახვა
- მომავალი ჯავშნის გაუქმება
- ელფოსტით შეტყობინება ჯავშნის დადასტურებისა და გაუქმებისას

**ადმინისტრატორისთვის**
- ყველა მომავალი (დადასტურებული) ჯავშნის ნახვა
- ნებისმიერი მომხმარებლის ჯავშნის გაუქმება

**ბიზნეს-წესები**
- ჯავშანი გრძელდება **2 საათი**
- წარსულ დროზე დაჯავშნა შეუძლებელია
- სტუმრების რაოდენობა ვერ აღემატება მაგიდის ტევადობას
- ერთი მაგიდა ერთსა და იმავე დროს ორჯერ ვერ დაიჯავშნება: გადაფარვის შემოწმება და ჩაწერა ხდება `Serializable` ტრანზაქციაში, რაც race condition-ს გამორიცხავს
- უკვე დაწყებული ან გასული ჯავშნის გაუქმება შეუძლებელია
- დრო ინახება UTC-ში, მომხმარებელს კი ეჩვენება რესტორნის დროის სარტყელში (ნაგულისხმევად `Asia/Tbilisi`)



##  ტექნოლოგიები

| სფერო | ტექნოლოგია |
|---|---|
| პლატფორმა | .NET 10 (შესაძლებელია .NET 8-ზე გადაყვანა) |
| UI | ASP.NET Core Razor Pages |
| მონაცემთა ბაზა | SQL Server + Entity Framework Core (Code First) |
| ავთენტიფიკაცია | JWT Bearer (HttpOnly, Secure, SameSite=Lax ქუქი) |
| პაროლის ჰეშირება | ASP.NET Core Identity `PasswordHasher` |
| ელფოსტა | MailKit (SMTP) |
| ტესტირება | xUnit + Moq |

## არქიტექტურა

```
Domain  <-  Application  <-  Infrastructure  <-  Web
                ^
              Tests
```

| პროექტი | პასუხისმგებლობა |
|---|---|
| **RestaurantBooking.Domain** | ენთითები: `User`, `Table`, `Reservation` და enum-ები (`UserRole`, `ReservationStatus`). არაფერზეა დამოკიდებული. |
| **RestaurantBooking.Application** | ბიზნეს-ლოგიკა (`AuthService`, `ReservationService`), ინტერფეისები (`IUserRepository`, `IReservationRepository`, `IEmailService`, `IJwtTokenGenerator`, `IPasswordHasher`, `IRestaurantClock`) და `Result` ტიპი. |
| **RestaurantBooking.Infrastructure** | ინტერფეისების რეალიზაცია: EF Core `AppDbContext` და რეპოზიტორიები, SMTP სერვისი, JWT გენერატორი, პაროლის ჰეშერი, დროის სარტყლის საათი, `DbSeeder`. |
| **RestaurantBooking.Web** | Razor Pages, JWT-ის კონფიგურაცია, ავტორიზაციის პოლიტიკები. |
| **RestaurantBooking.Tests** | Application ფენის unit ტესტები. |

### პროექტის სტრუქტურა

```
RestaurantBooking/
├── RestaurantBooking.sln
├── Directory.Build.props          # TargetFramework და პაკეტების ვერსიები
├── src/
│   ├── RestaurantBooking.Domain/
│   │   └── Entities/              # User, Table, Reservation
│   ├── RestaurantBooking.Application/
│   │   ├── Abstractions/          # ინტერფეისები
│   │   ├── Common/                # Result
│   │   └── Services/              # AuthService, ReservationService
│   ├── RestaurantBooking.Infrastructure/
│   │   ├── Auth/                  # JWT, პაროლის ჰეშირება
│   │   ├── Email/                 # SMTP (MailKit)
│   │   └── Persistence/           # AppDbContext, რეპოზიტორიები, DbSeeder
│   └── RestaurantBooking.Web/
│       ├── Auth/                  # ქუქის დამხმარე კლასები
│       └── Pages/
│           ├── Account/           # Login, Register, Logout, AccessDenied
│           ├── Reservations/      # ჩემი ჯავშნები, ახალი ჯავშანი
│           └── Admin/             # ყველა ჯავშანი (მხოლოდ ადმინი)
└── tests/
    └── RestaurantBooking.Tests/   # AuthServiceTests, ReservationServiceTests
```

### გვერდები და წვდომა

| მისამართი | აღწერა | წვდომა |
|---|---|---|
| `/` | მთავარი გვერდი | ყველა |
| `/Account/Register` | რეგისტრაცია | ყველა |
| `/Account/Login` | შესვლა | ყველა |
| `/Reservations` | ჩემი ჯავშნები | ავტორიზებული |
| `/Reservations/Create` | ახალი ჯავშანი | ავტორიზებული |
| `/Admin/Reservations` | ყველა მომავალი ჯავშანი | მხოლოდ `Admin` |

---

## 🗄️ მონაცემთა მოდელი

- **User** — `Id`, `FullName`, `Email` (უნიკალური), `PasswordHash`, `Role` (`Customer` / `Admin`)
- **Table** — `Id`, `Number` (უნიკალური), `Capacity`
- **Reservation** — `Id`, `UserId`, `TableId`, `StartTime`, `EndTime`, `GuestCount`, `Status` (`Confirmed` / `Cancelled`)

ბაზაში ავტომატურად ემატება 5 მაგიდა:

| მაგიდა № | ტევადობა |
|---|---|
| 1 | 2 |
| 2 | 2 |
| 3 | 4 |
| 4 | 4 |
| 5 | 6 |

---

## 🚀 გაშვება

### საჭიროა
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (ან .NET 8, იხ. ქვემოთ)
- SQL Server ან SQL Server LocalDB
- (სურვილისამებრ) Docker, ელფოსტის ლოკალური ტესტირებისთვის

### 1. რეპოზიტორიის კლონირება

```bash
git clone https://github.com/sabakiladze/Restaurant-Booking-System.git
cd Restaurant-Booking-System/RestaurantBooking
```

### 2. .NET ვერსიის შემოწმება

```bash
dotnet --version
```

თუ .NET 10 SDK არ გაქვს, `Directory.Build.props`-ში შეცვალე:
- `TargetFramework` → `net8.0`
- `MsVersion` → `8.0.*`
- `JwtLibVersion` → `7.1.2`

### 3. აწყობა

```bash
dotnet restore
dotnet build
```

### 4. მიგრაციის შექმნა

პროექტი Code First მიდგომას იყენებს და მიგრაციის ფაილები რეპოზიტორიაში ჯერ არ არის, ამიტომ პირველი მიგრაცია ხელით უნდა შექმნა:

```bash
dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialCreate \
  --project src/RestaurantBooking.Infrastructure \
  --startup-project src/RestaurantBooking.Web \
  --output-dir Persistence/Migrations
```

### 5. გაშვება

```bash
dotnet run --project src/RestaurantBooking.Web --launch-profile https
```

აპლიკაცია გაიხსნება მისამართზე: **https://localhost:7080** (ან http://localhost:5080).

Development გარემოში აპლიკაცია გაშვებისას თავად ასრულებს მიგრაციას და ქმნის ადმინისტრატორის ანგარიშს.

###  ადმინის ანგარიში (მხოლოდ dev)

| ელფოსტა | პაროლი |
|---|---|
| `admin@restaurant.local` | `Admin12345!` |

ეს მონაცემები `appsettings.Development.json`-შია.

---

## ⚙️ კონფიგურაცია

ძირითადი პარამეტრები `appsettings.json`-შია:

| გასაღები | აღწერა | ნაგულისხმევი |
|---|---|---|
| `ConnectionStrings:Default` | SQL Server-ის connection string | dev-ში LocalDB |
| `Jwt:Issuer` / `Jwt:Audience` | JWT-ის გამცემი / ადრესატი | `RestaurantBooking` / `RestaurantBooking.Web` |
| `Jwt:Key` | ხელმოწერის გასაღები (**მინიმუმ 32 სიმბოლო**) | dev-ში დროებითი |
| `Jwt:ExpiryMinutes` | ტოკენის ვადა წუთებში | `60` |
| `Smtp:Host` / `Port` / `User` / `Password` / `From` | SMTP სერვერის პარამეტრები | dev-ში `localhost:2525` |
| `Restaurant:TimeZone` | რესტორნის დროის სარტყელი | `Asia/Tbilisi` |
| `SeedAdmin:Email` / `Password` | ავტომატურად შესაქმნელი ადმინი | მხოლოდ dev |

საიდუმლო მნიშვნელობები (გასაღებები, პაროლები) appsettings-ში კი არა, **user-secrets**-ში ან გარემოს ცვლადებში შეინახე:

```bash
dotnet user-secrets set "Jwt:Key" "<გრძელი-შემთხვევითი-სტრიქონი>" --project src/RestaurantBooking.Web
```

---

##  ელფოსტა

### Development-ში (smtp4dev)

```bash
docker run -p 3000:80 -p 2525:25 rnwood/smtp4dev
```

გაგზავნილი წერილები ჩანს მისამართზე: **http://localhost:3000**

### რეალური SMTP

```bash
dotnet user-secrets set "Smtp:Host" "smtp.example.com" --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:Port" "587" --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:User" "..." --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:Password" "..." --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:From" "Restaurant <no-reply@yourdomain.ge>" --project src/RestaurantBooking.Web
```

> წერილის გაგზავნა რომც ჩავარდეს, ჯავშანი მაინც შეიქმნება. შეცდომა ლოგში ჩაიწერება.

---

##  ტესტები

```bash
dotnet test
```

ტესტები ამოწმებს Application ფენას (რეპოზიტორიები და სერვისები Moq-ით არის იმიტირებული):

**AuthService**
- მოკლე პაროლით რეგისტრაცია უარყოფილია
- უკვე დარეგისტრირებული ელფოსტა უარყოფილია
- ელფოსტა ნორმალიზდება, ბაზაში ინახება ჰეში და არა პაროლი
- უცნობ ელფოსტასა და არასწორ პაროლზე ერთი და იგივე შეცდომა ბრუნდება
- წარმატებული შესვლა აბრუნებს ტოკენს

**ReservationService**
- წარსულ დროზე, უცნობ მომხმარებელზე ან მაგიდაზე ჯავშანი უარყოფილია
- სტუმრების არასწორი რაოდენობა უარყოფილია
- დაკავებული მაგიდა უარყოფილია და წერილი არ იგზავნება
- წარმატებული ჯავშანი 2 საათიანია და მომხმარებელი წერილს იღებს
- ელფოსტის შეცდომა ჯავშანს არ აფუჭებს
- გაუქმება: მფლობელს და ადმინს შეუძლიათ, სხვა მომხმარებელს არა; დაწყებულ ან უკვე გაუქმებულ ჯავშანს ვერ გააუქმებ

---

##  უსაფრთხოება

- JWT ინახება `HttpOnly` + `Secure` + `SameSite=Lax` ქუქიში, ამიტომ JavaScript ვერ წაიკითხავს
- პაროლები ჰეშირდება ASP.NET Core Identity-ის `PasswordHasher`-ით
- შესვლისას შეცდომის ტექსტი ერთნაირია, რომ არ გამჟღავნდეს, რომელი ელფოსტაა დარეგისტრირებული
- წერილებში მომხმარებლის სახელი HTML-encode-ით გადის
- `/Admin` გვერდები დაცულია `AdminOnly` პოლიტიკით

---

##  პროდაქშენამდე გასაკეთებელი

- [ ] `Jwt:Key` გადაიტანე user-secrets-ში, გარემოს ცვლადში ან Key Vault-ში (dev-ის გასაღები დროებითია)
- [ ] ლოგინზე rate limiting (`AddRateLimiter`) brute-force-ისგან დასაცავად
- [ ] ელფოსტის დადასტურება რეგისტრაციისას და პაროლის აღდგენა
- [ ] Refresh ტოკენი (ახლა 60 წუთის შემდეგ თავიდან შესვლაა საჭირო)
- [ ] წერილების გაგზავნა outbox-ით ან background queue-თი (ახლა სინქრონულად იგზავნება)
- [ ] რესტორნის სამუშაო საათების ვალიდაცია

---

## 👤 ავტორი

**საბა კილაძე** — [@sabakiladze](https://github.com/sabakiladze)
