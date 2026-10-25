(() => {
  const media = window.matchMedia("(prefers-color-scheme: dark)");
  function selected() {
    try {
      return localStorage.getItem("mycal.theme") || "system";
    } catch {
      return "system";
    }
  }
  function apply() {
    document.documentElement.dataset.theme =
      selected() === "system" ? (media.matches ? "dark" : "light") : selected();
  }
  window.mycalTheme = {
    get: selected,
    set(value) {
      if (!["light", "dark", "system"].includes(value)) return;
      try {
        localStorage.setItem("mycal.theme", value);
      } catch {}
      apply();
    },
  };
  media.addEventListener("change", apply);
  apply();
})();
