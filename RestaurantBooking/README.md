# RestaurantBooking

Razor Pages · JWT (HttpOnly ქუქიში) · EF Core Code First (SQL Server) · Clean Architecture · xUnit · SMTP (MailKit)

```
Domain  <-  Application  <-  Infrastructure  <-  Web
                ^
              Tests
```

## გაშვება

```bash
dotnet --version                 # 10.x. თუ 8/9 — იხილე Directory.Build.props
dotnet restore
dotnet build

# EF tool + პირველი მიგრაცია (Code First; მიგრაციის ფაილები ჯერ არ არსებობს!)
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/RestaurantBooking.Infrastructure \
  --startup-project src/RestaurantBooking.Web \
  --output-dir Persistence/Migrations

# Development-ში აპლიკაცია თვითონ ამუშავებს მიგრაციას და ქმნის ადმინს
dotnet run --project src/RestaurantBooking.Web --launch-profile https
```

ადმინი (dev): `admin@restaurant.local` / `Admin12345!` (appsettings.Development.json).

## SMTP dev-ში
```bash
docker run -p 3000:80 -p 2525:25 rnwood/smtp4dev     # წერილები: http://localhost:3000
```
რეალური SMTP-სთვის:
```bash
dotnet user-secrets set "Smtp:Host" "smtp.example.com" --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:Port" "587" --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:User" "..." --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:Password" "..." --project src/RestaurantBooking.Web
dotnet user-secrets set "Smtp:From" "Restaurant <no-reply@yourdomain.ge>" --project src/RestaurantBooking.Web
```

## ტესტები
```bash
dotnet test
```

## პროდაქშენამდე აუცილებელია
- `Jwt:Key` — user-secrets / env variable / Key Vault. appsettings.Development.json-ის გასაღები დროებითია.
- ლოგინზე rate limiting (`AddRateLimiter`) — ახლა brute-force-ისგან დაცული არ არის.
- ელფოსტის დადასტურება რეგისტრაციისას; პაროლის აღდგენა.
- JWT-ს refresh ტოკენი არ აქვს: 60 წუთში მომხმარებელი თავიდან უნდა შევიდეს.
- ჯავშნის წერილი იგზავნება სინქრონულად (ჩავარდნა იჭერება და იწერება ლოგში) — უკეთესია outbox/background queue.
- მუშაობის საათების ვალიდაცია ჯერ არ არის.
