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
  const errors = document.querySelector(".validation[role='alert']");
  const result = document.getElementById("results-heading");
  const focusTarget = errors || result;
  if (focusTarget) {
    focusTarget.focus({ preventScroll: true });
    focusTarget.scrollIntoView({ block: "start" });
  }
});
