# Publicação no Azure App Service F1

O [site público](https://issuepilot-jorge-2026-hhb0cpeudnf3ezdd.canadacentral-01.azurewebsites.net/) foi publicado em 16/11/2026 no Web App `issuepilot-jorge-2026`, grupo `rg-sistemaestoque`, assinatura Azure for Students. Usa **Windows**, **.NET 10** e o plano existente **Free F1** (`ASP-rgsistemaestoque-8071`). HTTPS está obrigatório. Nenhum plano pago foi ativado para o IssuePilot.

## Limites reais do F1

- A [tabela oficial de preços](https://azure.microsoft.com/en-us/pricing/details/app-service/windows/) indica 60 minutos de CPU por dia, 1 GB de armazenamento e ausência de SLA. É adequado a uma demonstração de portfólio, sujeito às cotas.
- O [guia de desempenho da Microsoft](https://learn.microsoft.com/en-us/troubleshoot/azure/app-service/troubleshoot-performance-slow-web-app) informa que **Always On não está disponível em Free/Shared**. Após inatividade, a primeira visita pode demorar; o F1 não garante resposta imediata 24 horas por dia.
- A [documentação do App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-common) informa que, sem Always On, o aplicativo é descarregado após 20 minutos sem requisições.

## Configuração segura

1. O código está no GitHub, os 23 testes xUnit passaram em Release e o pacote publicado foi inspecionado para excluir arquivos de banco e segredos.
2. O ZIP foi publicado no Web App existente com OneDeploy. A página inicial, `/openapi/v1.json`, os dois modos de análise e a rejeição de host falso foram testados por HTTPS em 16/11/2026. O modo observado foi `Regras locais — chave Gemini ausente`.
3. `GEMINI_API_KEY` foi configurada em **Settings > Environment variables > App settings** do Web App, sem inserir o valor no repositório, `appsettings.json`, frontend ou screenshots. O provedor e o modelo padrão são Gemini e `gemini-3.5-flash-lite`.
4. Em 16/11/2026, após a configuração, os dois modos responderam no site público com `analysisMode: gemini-3.5-flash-lite` e Markdown completo. Confira se o projeto da chave Gemini continua no plano gratuito e acompanhe os limites de uso. O aplicativo aplica limite de 20 requisições por minuto por IP, mas isso não substitui as cotas do provedor.

O SQLite do MVP guarda apenas data, modo e nota. No App Service, o arquivo é criado em `%HOME%/data/IssuePilot` para ficar fora do diretório publicado. A persistência após publicações e reinícios deve ser verificada; não trate esse histórico como banco de produção. O funcionamento principal do relatório não depende do histórico armazenado.
