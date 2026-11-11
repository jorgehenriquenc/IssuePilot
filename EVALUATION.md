# Avaliação com issues públicas reais — 10/11/2026

O fluxo de URL foi executado pela aplicação local, que consultou a GitHub REST API. Os resultados de 10/11 abaixo vieram do analisador de regras locais. São saídas reais desta execução, não métricas de desempenho do produto.

| Issue | Resultado observado |
|---|---|
| [dotnet/aspnetcore #70409](https://github.com/dotnet/aspnetcore/issues/70409) | HTTP 200; relatório gerado; nota 85/100; confiança 0,35; uma possível duplicata retornada pela busca do GitHub; lacuna apontada: evidência/logs. |
| [dotnet/aspnetcore #70416](https://github.com/dotnet/aspnetcore/issues/70416) | HTTP 200; relatório gerado; nota 100/100 pelo heurístico local. |

A [#70409](https://github.com/dotnet/aspnetcore/issues/70409) já descreve passos numerados, comportamento esperado e observado e ambiente. A [#70416](https://github.com/dotnet/aspnetcore/issues/70416) traz análise técnica e um caso de reprodução detalhado. Esses exemplos verificam a ingestão e a apresentação, mas ainda não validam a qualidade das sugestões do Gemini nem a precisão das duplicatas. A nota local é baseada na presença de campos e deve ser lida com a confiança baixa indicada pelo app.

O teste HTTP local também verificou um rascunho: `GET /` retornou 200, o formulário com antiforgery token gerou relatório em `POST /Home/Analyze`, e uma URL com host `github.com.evil.test` foi rejeitada. A especificação OpenAPI 3.1.1 contém os endpoints `/api/analysis/draft` e `/api/analysis/url`.

Um teste adicional submeteu `<script>alert(1)</script>` em um rascunho: o HTML de resposta não continha a tag executável e continha `&lt;script&gt;`. Um POST sem antiforgery token retornou HTTP 400. Esses testes cobrem os caminhos exercitados, não substituem uma revisão de segurança completa.

Após a revisão motivada pelos prints do usuário, a execução da versão Release com a #70409 passou a escolher o parágrafo do bug como resumo, extrair seis passos reais de reprodução e descartar a sugestão de duplicata sem semelhança suficiente. Sem chave no processo de teste, o relatório indicou `Regras locais — chave Gemini ausente`.

## Execução com Gemini — 11/11/2026

O usuário analisou novamente a [#70409](https://github.com/dotnet/aspnetcore/issues/70409) e compartilhou capturas do resultado. A tela mostrou `Modo: gemini-3.5-flash-lite`, nota de qualidade 95/100, confiança estimada pelo modelo de 100%, categoria Bug, prioridade Medium e impacto High. O relatório trouxe seis passos de reprodução, comportamento esperado e observado, uma hipótese identificada como hipótese e nenhuma duplicata sugerida. Esses números são saídas de uma única análise, não medidas de precisão ou desempenho.

A mesma captura revelou `improvedMarkdown: Não informado`, embora o restante do relatório tivesse conteúdo. Foi acrescentada uma validação pós-modelo: se o campo não contiver um rascunho Markdown com título e seções, a aplicação monta o rascunho localmente a partir da issue e mostra um aviso. Esse comportamento passou em teste automatizado; ainda precisa ser conferido no navegador após reiniciar o app. A interface passou a chamar a confiança de estimativa.

## Verificações adicionais — 11/11/2026

- A suíte xUnit passou com **23 testes** após as correções. Isso verifica código local e respostas HTTP simuladas; não mede a precisão do Gemini.
- A auditoria Lighthouse mobile da página inicial e de um resultado local terminou com **100 em acessibilidade, boas práticas, SEO e navegação por agente** em ambas as telas testadas. O primeiro exame encontrou contraste insuficiente em textos secundários e um estilo inline bloqueado pela CSP; ambos foram corrigidos antes da medição final. A auditoria não substitui testes manuais completos com leitor de tela.
- O pacote de publicação iniciado localmente respondeu HTTP 200 na página inicial e expôs OpenAPI 3.1.1. A análise da [#70409](https://github.com/dotnet/aspnetcore/issues/70409) sem chave retornou seis passos e Markdown com título; URL fora de `github.com` e rascunho com padrão de segredo retornaram HTTP 400.
- O ZIP final foi inspecionado e não contém arquivo SQLite, chave, certificado ou segredo. Nenhum deploy remoto foi executado.
