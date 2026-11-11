# Publicação sem cobrança

O projeto ainda não foi publicado. O primeiro caminho a verificar é **Azure App Service F1 para Windows**, com runtime **.NET 10** e publicação de código. Confirme na tela final do Azure que o plano é **Free F1** e que o custo estimado é **$0** antes de criar qualquer recurso. Não aceite uma troca automática para Basic, Standard ou outro plano pago.

## Limites reais do F1

- A [tabela oficial de preços](https://azure.microsoft.com/en-us/pricing/details/app-service/windows/) indica 60 minutos de CPU por dia, 1 GB de armazenamento e ausência de SLA. É adequado a uma demonstração de portfólio, sujeito às cotas.
- O [guia de desempenho da Microsoft](https://learn.microsoft.com/en-us/troubleshoot/azure/app-service/troubleshoot-performance-slow-web-app) informa que **Always On não está disponível em Free/Shared**. Após inatividade, a primeira visita pode demorar; o F1 não garante resposta imediata 24 horas por dia.
- A [documentação do App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-common) informa que, sem Always On, o aplicativo é descarregado após 20 minutos sem requisições.

## Configuração segura

1. Publique o código no GitHub depois de conferir o conjunto de arquivos e executar `dotnet test`.
2. Na conta Azure, crie um Web App com **Windows**, **Code**, **.NET 10** e **Free F1**, seguindo o [guia oficial](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore). Se F1 não estiver disponível na região/assinatura, pare antes de criar um plano pago.
3. Configure `GEMINI_API_KEY` somente em **Environment variables / App settings** do Web App. Configure `ISSUEPILOT_MODEL_PROVIDER=gemini` e `ISSUEPILOT_GEMINI_MODEL=gemini-3.5-flash-lite`. Não coloque valores reais no repositório, `appsettings.json`, frontend ou screenshots.
4. Confira se o projeto da chave Gemini continua no plano gratuito e acompanhe os limites de uso. O aplicativo aplica limite de 20 requisições por minuto por IP, mas isso não substitui as cotas do provedor.
5. Após o deploy, teste `/`, `/openapi/v1.json`, os dois modos de análise e o relatório de uma issue pública. Registre o link público no README e na candidatura somente depois desses testes.

O SQLite do MVP guarda apenas data, modo e nota. No App Service, o arquivo é criado em `%HOME%/data/IssuePilot` para ficar fora do diretório publicado. A persistência após publicações e reinícios deve ser verificada; não trate esse histórico como banco de produção. O funcionamento principal do relatório não depende do histórico armazenado.
