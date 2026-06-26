# Sapient Sensor Simulator

Pieni komentorivityökalu, joka esiintyy SAPIENT Edge Node -sensorina: yhdistää ulospäin annettuun host:porttiin ja lähettää validia BSI Flex 335 v2.0 -dataa Apexin verkkokehystyksellä. Tarkoitettu [sapient-fusion-node](../sapient-fusion-node)-projektin (ja Apex-SAPIENT-Middlewaren) testaamiseen, kun oikeaa sensoria ei ole käsillä.

## Mitä se tekee

1. Yhdistää TCP:n yli `--host:--port`-osoitteeseen.
2. Lähettää yhden `Registration`-sanoman (täyttää kaikki BSI Flex 335 v2.0:n pakolliset kentät, mutta sisältö on muuten keksitty — ei oikeaa ASM:ää tämän takana).
3. Lähettää jatkuvasti `DetectionReport`-sanomia simuloiduille kohteille, jotka liikkuvat ympyrää origon ympäri (lat/lon + `ENUVelocity`).

Verkkokehystys (4 tavun little-endian pituusetuliite + raaka protobuf-tavusarja) on identtinen Apex-SAPIENT-Middlewaren ja sapient-fusion-noden kanssa — `SapientWireCodec`-tiedosto on tarkoituksella pidetty samanlaisena molemmissa repoissa.

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

5 testiä kattaa: Registration-sanoman pakollisten kenttien täyttymisen, DetectionReport-sanoman round-trip-serialisoinnin, simuloidun kohteen liikeradan, ja koko TCP-langan yli tapahtuvan kehystyksen/dekoodauksen.

## Tunnetut rajoitukset

- Ei odota tai käsittele `RegistrationAck`-vastausta.
- Ei lähetä `StatusReport`-heartbeat-sanomia.
- Kohteet liikkuvat aina täydellistä ympyrää — ei realistisempia liikeratamalleja.
- Vain `BSI Flex 335 v2.0` / Proto-formaatti, ei XML- tai v1.0-tukea.
