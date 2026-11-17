const systemTheme = window.matchMedia("(prefers-color-scheme: dark)");
let themeChoice = "auto";
try {
  const savedTheme = localStorage.getItem("issuepilot-theme");
  if (savedTheme === "light" || savedTheme === "dark") themeChoice = savedTheme;
} catch { /* Private browsing can restrict local storage. */ }

function applyTheme() {
  if (themeChoice === "auto") document.documentElement.removeAttribute("data-theme");
  else document.documentElement.dataset.theme = themeChoice;
  const dark = themeChoice === "dark" || (themeChoice === "auto" && systemTheme.matches);
  document.querySelector('meta[name="theme-color"]')?.setAttribute("content", dark ? "#101b1d" : "#f5f6f2");
  const toggle = document.querySelector("[data-theme-toggle]");
  const label = document.querySelector("[data-theme-label]");
  const names = { auto: "Sistema", light: "Claro", dark: "Escuro" };
  if (label) label.textContent = names[themeChoice];
  if (toggle) toggle.setAttribute("aria-label", `Tema: ${names[themeChoice]}. Alterar tema`);
}
applyTheme();
systemTheme.addEventListener("change", applyTheme);

document.addEventListener("DOMContentLoaded", () => {
  applyTheme();
  document.querySelector("[data-theme-toggle]")?.addEventListener("click", () => {
    themeChoice = { auto: "light", light: "dark", dark: "auto" }[themeChoice];
    try {
      if (themeChoice === "auto") localStorage.removeItem("issuepilot-theme");
      else localStorage.setItem("issuepilot-theme", themeChoice);
    } catch { /* The selected theme still works for this visit. */ }
    applyTheme();
  });

  const stages = [
    { kicker: "01 / ENTRADA", message: "Uma issue pública ou um rascunho. O ponto de partida é o seu relato." },
    { kicker: "02 / CLAREZA", message: "Contexto, lacunas e evidências organizados para entender o problema." },
    { kicker: "03 / AÇÃO", message: "Um rascunho revisável e próximos passos para decidir com confiança." }
  ];
  const stageButtons = [...document.querySelectorAll("[data-stage]")];
  const stagePanel = document.querySelector(".graphic-stage");
  const stageKicker = document.querySelector("[data-stage-kicker]");
  const stageMessage = document.querySelector("[data-stage-message]");
  const stageIndex = document.querySelector("[data-stage-index]");
  const stageRoute = document.querySelector(".graphic-route");
  function showStage(index) {
    const stage = stages[index];
    if (!stage) return;
    if (stageKicker) stageKicker.textContent = stage.kicker;
    if (stageMessage) stageMessage.textContent = stage.message;
    if (stageIndex) stageIndex.textContent = `0${index + 1} — 03`;
    if (stageRoute) stageRoute.style.setProperty("--route-progress", `${index * 50}%`);
    stageButtons.forEach((button, position) => button.setAttribute("aria-pressed", String(position === index)));
    stagePanel?.classList.remove("is-changing");
    void stagePanel?.offsetWidth;
    stagePanel?.classList.add("is-changing");
  }
  stageButtons.forEach((button, index) => {
    button.addEventListener("click", () => showStage(index));
    button.addEventListener("keydown", event => {
      if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
      event.preventDefault();
      const next = (index + (event.key === "ArrowRight" ? 1 : -1) + stageButtons.length) % stageButtons.length;
      stageButtons[next].focus();
      showStage(next);
    });
  });

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
