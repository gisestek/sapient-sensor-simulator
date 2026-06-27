# Sapient Sensor Simulator

Pieni komentorivityökalu, joka esiintyy SAPIENT Edge Node -sensorina: yhdistää ulospäin annettuun host:porttiin ja lähettää validia BSI Flex 335 v2.0 -dataa Apexin verkkokehystyksellä. Tarkoitettu [sapient-fusion-node](../sapient-fusion-node)-projektin (ja Apex-SAPIENT-Middlewaren) testaamiseen, kun oikeaa sensoria ei ole käsillä.

## Mitä se tekee

1. Yhdistää TCP:n yli `--host:--port`-osoitteeseen.
2. Lähettää yhden `Registration`-sanoman (täyttää kaikki BSI Flex 335 v2.0:n pakolliset kentät, mutta sisältö on muuten keksitty — ei oikeaa ASM:ää tämän takana).
3. Lähettää jatkuvasti `DetectionReport`-sanomia simuloiduille kohteille, jotka liikkuvat ympyrää origon ympäri (lat/lon + `ENUVelocity`).

Verkkokehystys (4 tavun little-endian pituusetuliite + raaka protobuf-tavusarja) on identtinen Apex-SAPIENT-Middlewaren ja sapient-fusion-noden kanssa — `SapientWireCodec`-tiedosto on tarkoituksella pidetty samanlaisena molemmissa repoissa.

**Kohteen liikerata on puhtaasti seinäkellon (Unix-ajan) funktio**, ei "aika siitä kun tämä prosessi käynnistyi". Tämä tarkoittaa, että kaksi simulaattori-instanssia samoilla kohdeparametreilla ja synkronoiduilla kelloilla — vaikka eri tietokoneilla — laskevat täsmälleen saman radan täsmälleen samalla hetkellä. Sapient-fusion-noden `TrackManager` voi siis aidosti yhdistää ne saman maalin havainnoiksi sijainnin+ajan perusteella, ilman että `object_id`:t (jotka ovat aina satunnaisia per prosessi) täsmäävät.

## Käyttö

```
dotnet run --project src/SapientSensorSimulator --port 5020
```

Vaihtoehdot:

```
--host <host>        Kohdeosoite (oletus: 127.0.0.1)
--port <port>        Kohdeportti (pakollinen)
--targets <n>         Simuloitujen kohteiden määrä (oletus: 2)
--interval-ms <ms>    Aika havaintoerien välillä (oletus: 1000)
--origin-lat <deg>    Simuloitujen kohteiden origon leveysaste (oletus: 60.1699)
--origin-lon <deg>    Simuloitujen kohteiden origon pituusaste (oletus: 24.9384)
--name <name>         Registration "name" -kenttä
--noise field:kind:magnitude
                      Lisää virhettä yhteen kenttään ennen lähetystä. Toistettavissa.
                      Kentät: East, North, Altitude, EastRate, NorthRate, UpRate (metriä / m/s)
                      Tyypit: Gaussian    (magnitude = keskihajonta)
                              Uniform     (magnitude = +/- vaihteluväli)
                              Systematic  (magnitude = deterministisen sinikäyrän amplitudi,
                                           ajettu seinäkellon mukaan — "säännönmukaista", ei satunnaista)
                      Esim: --noise East:Gaussian:2.5 --noise Altitude:Systematic:1.0
--error field:magnitude (metriä)
                      Asettaa raportoidun Location x/y/z_error -arvon ("fixed"-tila), ohittaen
                      automaattisen oletuksen. Ilman --error: kenttä jolla on --noise raportoi
                      automaattisesti sen kohinan oman magnitudin epävarmuutena ("auto"-tila,
                      oletus); kenttä jolla ei ole kumpaakaan ei raportoi epävarmuutta lainkaan.
                      Toistettavissa, yksi per kenttä. Kentät: East, North, Altitude.
                      Esim: --error East:5.0 --error North:5.0
```

## Testaus yhdessä sapient-fusion-noden kanssa

1. Käynnistä sapient-fusion-node, luo Yhteydet-välilehdellä yhteys `Outbound = false` (kuuntelija) jollekin portille ja paina Yhdistä.
2. Aja tämä simulaattori `--port`-parametrilla siihen samaan porttiin.
3. Havainnot ilmestyvät sapient-fusion-noden "Raakadata 3D" -näkymään (`SensorConnectionManager.MessageReceived` → `DetectionReportConverter`).

## Apex-SAPIENT-Middleware

`external/apex` on git-submoduleksi tuotu [dstl/Apex-SAPIENT-Middleware](https://github.com/dstl/Apex-SAPIENT-Middleware) (Apache 2.0) — käytetään vain BSI Flex 335 v2.0 -protobuf-skeemoista, joista `SapientSensorSimulator.csproj` generoi C#-luokat build-aikana. Apexin Python-koodia ei käännetä tai ajeta tästä repositoriosta.

## Build & testit

```
dotnet build
dotnet test
```

### Itsenäinen .exe

```
dotnet publish src/SapientSensorSimulator -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

Tuottaa `publish/win-x64/SapientSensorSimulator.exe` — yksitiedostoinen, itsenäinen ohjelma, ei vaadi .NET-asennusta kohdekoneelta. `publish/` on `.gitignore`:ssä, koska binääri on iso (~75 MB); jaa se erikseen (esim. GitHub Release) tarvittaessa.

29 testiä kattaa: Registration-sanoman pakollisten kenttien täyttymisen, DetectionReport-sanoman round-trip-serialisoinnin (mukaan lukien x/y/z_error-kenttien Has*-lippujen oikeellisuus), simuloidun kohteen liikeradan (mukaan lukien determinismi: sama aika+parametrit → sama tulos eri instansseissa), kohinageneraattorin kaikki kolme tyyppiä, `--noise`/`--error`-argumenttien jäsennyksen ja auto/fixed-päättelyn, ja koko TCP-langan yli tapahtuvan kehystyksen/dekoodauksen.

## Tunnetut rajoitukset

- Ei odota tai käsittele `RegistrationAck`-vastausta.
- Ei lähetä `StatusReport`-heartbeat-sanomia.
- Kohteet liikkuvat aina täydellistä ympyrää — ei realistisempia liikeratamalleja.
- Vain `BSI Flex 335 v2.0` / Proto-formaatti, ei XML- tai v1.0-tukea.
- `--error`/auto-epävarmuus kattaa vain `East`/`North`/`Altitude` (→ Location x/y/z_error) — ei `EastRate`/`NorthRate`/`UpRate`-kenttien epävarmuutta (SAPIENT-skeemassa ei ole vastaavia velocity_error-kenttiä `ENUVelocity`-sanomassa).
