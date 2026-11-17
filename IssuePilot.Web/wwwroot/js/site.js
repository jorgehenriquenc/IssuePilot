document.addEventListener("DOMContentLoaded", () => {
  const modeInputs = document.querySelectorAll('input[name="Mode"]');
  const urlFields = document.getElementById("url-fields");
  const draftFields = document.getElementById("draft-fields");
  const url = document.getElementById("url");
  const title = document.getElementById("title");
  const body = document.getElementById("body");
  const submitLabel = document.getElementById("submit-label");
  if (!urlFields || !draftFields) return;

  function updateMode() {
    const isUrl = document.querySelector('input[name="Mode"][value="url"]')?.checked;
    urlFields.hidden = !isUrl;
    draftFields.hidden = !!isUrl;
    url.required = !!isUrl;
    title.required = !isUrl;
    body.required = !isUrl;
    url.disabled = !isUrl;
    title.disabled = !!isUrl;
    body.disabled = !!isUrl;
    if (submitLabel) submitLabel.textContent = isUrl ? "Analisar issue" : "Melhorar rascunho";
  }

  modeInputs.forEach(input => input.addEventListener("change", updateMode));
  updateMode();
  const form = document.querySelector(".input-panel form");
  form?.addEventListener("submit", () => {
    if (!form.checkValidity()) return;
    const button = form.querySelector('button[type="submit"]');
    if (button) button.disabled = true;
    if (submitLabel) submitLabel.textContent = "Analisando relato…";
  });

  const copyButton = document.querySelector("[data-copy-markdown]");
  copyButton?.addEventListener("click", async () => {
    const markdown = document.getElementById("markdown-content")?.textContent;
    const status = document.getElementById("copy-status");
    if (!markdown) return;
    try {
      await navigator.clipboard.writeText(markdown);
      if (status) status.textContent = "Rascunho copiado para a área de transferência.";
      copyButton.firstChild.textContent = "Copiado ";
      window.setTimeout(() => { copyButton.firstChild.textContent = "Copiar texto "; }, 2500);
    } catch {
      if (status) status.textContent = "Não foi possível copiar automaticamente. Selecione o texto do rascunho para copiar.";
    }
  });
  const errors = document.querySelector(".validation[role='alert']");
  const result = document.getElementById("results-heading");
  const focusTarget = errors || result;
  if (focusTarget) {
    focusTarget.focus({ preventScroll: true });
    focusTarget.scrollIntoView({ block: "start" });
  }
});
