# Publicando uma nova versão

Toda versão é criada manualmente via git tag. Quando a tag é empurrada para o GitHub, o pipeline publica automaticamente as três imagens no registry com os novos rótulos.

## Formato da versão

As versões seguem [Semantic Versioning](https://semver.org):

```
v<major>.<minor>.<patch>
```

| Parte | Quando incrementar |
|---|---|
| `major` | Mudança incompatível — ex: schema do banco alterado, endpoint removido |
| `minor` | Funcionalidade nova compatível — ex: novo endpoint, novo consumer |
| `patch` | Correção de bug sem impacto na interface |

Exemplos válidos: `v1.0.0`, `v1.4.5`, `v2.0.0`.

## Passo a passo

**1. Certifique-se de estar em `main` e com o código atualizado:**

```sh
git checkout main
git pull origin main
```

**2. Crie a tag anotada:**

```sh
git tag -a v1.4.5 -m "Release v1.4.5"
```

A flag `-a` cria uma tag anotada, que carrega autor, data e mensagem — preferível a tags simples.

**3. Envie a tag para o GitHub:**

```sh
git push origin v1.4.5
```

Ou, para enviar todas as tags locais pendentes de uma vez:

```sh
git push origin --tags
```

## O que acontece após o push da tag

O GitHub Actions detecta a tag `v*.*.*` e executa o job `docker` em paralelo para os três targets. As imagens são publicadas com quatro rótulos cada:

```
ghcr.io/elyosemite/demografana/api:v1.4.5   ← versão exata
ghcr.io/elyosemite/demografana/api:1.4       ← major.minor
ghcr.io/elyosemite/demografana/api:latest    ← sempre aponta para o último push em main
ghcr.io/elyosemite/demografana/api:<sha>     ← commit exato
```

O mesmo vale para `/relay` e `/worker`.

## Corrigindo uma tag publicada por engano

Se a tag foi empurrada com a mensagem ou o commit errado, remova-a localmente e no remoto antes de recriar:

```sh
git tag -d v1.4.5
git push origin --delete v1.4.5
```

Depois repita o passo a passo com a tag correta.
