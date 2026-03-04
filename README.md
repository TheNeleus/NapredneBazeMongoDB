# 🛡️ RPG Avantura - Web Aplikacija

Aplikacija koja simulira ekonomiju RPG igre, uključujući inventar igrača, otvaranje "Loot Box" kutija, kreiranje klanova i "real-time" aukcijsku kuću sa transakcijama.
Projekat za predmet: Napredne Baze Podataka.

## 🚀 Tehnologije

**Backend:**
* **.NET 8 (C#)** - Web API i poslovna logika.
* **MongoDB** (NoSQL Document Database) - Konfigurisana kao **Replica Set** kako bi podržala ACID transakcije pri kupovini i prodaji predmeta.
* **Background Services** (HostedService) - Pozadinski "radnik" koji svake minute automatski proverava i razrešava istekle aukcije.
* **Docker** - Za hostovanje baze podataka.

**Frontend:**
* Vanilla JavaScript (ES6+ Asinhroni kod sa Fetch API)
* HTML5 & CSS3 (Custom 3D RPG Tema)
* Bootstrap 5 (UI komponente)

---

## 🛠️ Kako pokrenuti projekat

### 1. Preduslovi
Potrebno je imati instalirano:
* [Docker Desktop](https://www.docker.com/)
* [.NET 8 SDK](https://dotnet.microsoft.com/)
* [Visual Studio Code](https://code.visualstudio.com/) sa instaliranom ekstenzijom **Live Server**.

### 2. Pokretanje Infrastrukture (Baze)

U root folderu projekta otvorite terminal i pokrenite Docker kontejner:
```bash
docker compose up -d
``` 

⚠️ VAŽAN KORAK (Inicijalizacija Replica Set-a):
Pošto se koriste transakcije, MongoDB mora da radi u Replica Set režimu. Sačekajte par sekundi da se kontejner podigne, a zatim u terminalu obavezno pokrenite ovu komandu da biste ga inicijalizovali:

```bash
docker exec -it rpg-mongo-rs mongosh --eval "rs.initiate({_id: 'rs0', members: [{_id: 0, host: 'localhost:27017'}]})"
```
(Očekivani odgovor je da sadrži "ok": 1)

### 3. Pokretanje Backenda
Pozicionirajte se u folder backend projekta (gde se nalazi .csproj fajl) i pokrenite API:

```bash
dotnet watch run
```
Backend će se pokrenuti na portu 5228.
Možete pristupiti bazi i dodati početne "Loot Box"-ove i "Item"-e preko Swagger interfejsa na adresi: http://localhost:5228/swagger

### 4. Pokretanje Frontenda
Pošto frontend ne koristi Node.js (Vanilla JS), pokreće se direktno preko pretraživača:

Otvorite folder frontenda u Visual Studio Code-u.

Otvorite fajl index.html.

Kliknite na dugme "Go Live" u donjem desnom uglu VS Code prozora (zahteva instaliranu Live Server ekstenziju).

Igra će se automatski otvoriti u vašem podrazumevanom pretraživaču.