# Changelog

## 1.0.0 (2026-08-10)


### Features

* add health checks for AdminPanel and shared Postgres, Redis ([51a3335](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/51a333568ee813a74b42cb878867610bfa4c87ed))
* add opentelemetry ([85595a4](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/85595a48d3de214705f7806354a715d0c1f2b9ed))
* flag for logging ([8f7455c](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/8f7455cf2b7f41cc54ba0a2d80206b07a4ddb1e8))
* load tests ci ([ca6db70](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/ca6db7049a77bf7771ce61fee2ac2d52f1f05fc3))
* migrate authentication to Keycloak OIDC ([c6a09a3](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/c6a09a3ad961396a69a7a92bb583f1183d417d6a))
* unify API error handling and add standalone docker-compose ([bf26ced](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/bf26ced1d72458ecca89b7d9e9887a4a27f3d7ad))
* verbose logs ([ce11b6f](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/ce11b6f85aa71178d110bdd3bed5e41d60da0d84))
* verbose logs ([207e456](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/207e4567dc8bd214b25f822ded52b4abb4879cd8))
* verify OxaPay webhook signatures instead of polling-only ([6042d6f](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/6042d6fa845c733bd251c4575366c53d224e2e1d))


### Bug Fixes

* **ci:** address SonarQube findings on release.yml ([3c73614](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/3c73614158c48a1282aed7c10cd85dee05a2cffc))
* **ci:** fix deploy workflow's missing Keycloak realm-export staging ([2f74686](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/2f746862e096a98a7b616ad44f1b36ca48212472))
* **ci:** pass VITE_OIDC_* build args to admin-client image ([3259120](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/3259120b4beb38eb3b62060a1b0a2d8a781f8d35))
* deploy ([f5cca84](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/f5cca84ddf9a3a5042146470cb344e869bc1c3cb))
* deploy pipeline ([8ad5e45](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/8ad5e45373b73bda80c0bc471132ee471f4df25d))
* deploy pipeline ([a21aa72](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/a21aa726854b782611e013cd71a0db771aefb04f))
* deploy sonarqube coverage ([e457010](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/e457010d24d5f3c9adb44e0bad268779ea5c20d7))
* force re-login when Keycloak silent token renewal fails ([069b8b6](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/069b8b6c3b53fb596ff0e2fb6d6a834035cf2563))
* integration test pipeline ([f4afe6b](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/f4afe6be9ec5734f230669fa15d155a2cdb70ace))
* load test ([a4fe9b8](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/a4fe9b81747c7014db11a1f028e40e6fee3fd22c))
* pipeline ([12a5971](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/12a59718d0fcfa3cfcb10b5e8eda0bce33d0a53d))
* pipeline ([644ef43](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/644ef43a85d98dee19b6503a66d056c0ee9c7cc5))
* pipeline build ([a90afec](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/a90afece92d6f08227873740b774fa23a5b55360))
* repin deploy-service.yml to pick up client test publish fix ([79a0985](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/79a098532da16c905ff78ea50b597137ba5be6f9))
* repin deploy-service.yml to the corrected working-directory fix ([42c6522](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/42c65222ddaf33fed9d75436812b9313113b766c))
* stage monorepo keycloak/realm-export in CI for KeycloakTestFixture ([09f3e43](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/09f3e439ed9f5754c401494fcf5108bf129573ae))
* temporary to get logs ([8a78c1c](https://github.com/dimasdom/ArbiSpreadScanner.AdminPannel/commit/8a78c1cd64d21f2b91d01837db7cd0e86f0f5342))
