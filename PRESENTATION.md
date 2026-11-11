# Apresentação da candidatura

## Screenshots a capturar

1. Entrada em desktop, com os dois modos visíveis.
2. Resultado de uma issue pública real, incluindo qualidade, lacunas e duplicatas.
3. Resultado de um rascunho, com Markdown melhorado.
4. Tela estreita em celular, com navegação por teclado demonstrada no vídeo.

As imagens são capturas reais do app rodando no Chrome. [Entrada desktop](docs/screenshots/desktop-entrada.png) e [entrada mobile](docs/screenshots/mobile-entrada.png) já foram capturadas sem chave ou dados pessoais. O app roda localmente em `http://127.0.0.1:5191/`. Antes da captura do resultado com Gemini, conferir no navegador o rascunho Markdown após a correção de 11/11.

## Roteiro de vídeo (60–90 segundos)

1. **Problema:** issues incompletas atrasam a triagem.
2. **Demonstração:** colar URL pública do GitHub; mostrar resumo, qualidade e dados ausentes.
3. **Segundo modo:** escrever rascunho e mostrar Markdown melhorado.
4. **Segurança:** URL restrita, detecção de segredos, saída validada, nenhuma escrita no GitHub.
5. **Tecnologia:** ASP.NET Core MVC, monólito modular, GitHub REST API, Gemini com opção Ollama e testes.
6. **Fecho:** decisão final sempre humana; próximos passos após o MVP.

## Texto para LinkedIn

Construí o IssuePilot para transformar relatos de bugs em ações claras. O MVP analisa issues públicas do GitHub e ajuda a melhorar rascunhos antes da publicação. A solução foi feita em C# e ASP.NET Core, com módulos separados para triagem, qualidade, busca de possíveis duplicatas e revisão. Segurança guiou as decisões: URLs restritas, detecção de segredos, validação da saída da IA e nenhuma modificação automática no GitHub. Estou usando o projeto para aprofundar minha prática em GenAI aplicada a fluxos reais de desenvolvimento.

## Currículo

**IssuePilot — projeto pessoal (C#, ASP.NET Core, GitHub REST API, Gemini/Ollama):** desenvolvi MVP para triagem de issues e melhoria de rascunhos, com validação de entradas e saídas, proteção contra prompt injection, interface responsiva, testes xUnit e workflow de CI configurado em GitHub Actions.
