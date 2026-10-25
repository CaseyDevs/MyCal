let cleanup;
export function close() {
  cleanup?.();
  cleanup = undefined;
}
export function open(selector, callback) {
  close();
  const dialog = document.querySelector(selector);
  if (!dialog) return;
  const previous = document.activeElement;
  const previousOverflow = document.body.style.overflow;
  document.body.style.overflow = "hidden";
  const background = [
    ...document.querySelectorAll(".sidebar, .main-content > *"),
  ].filter((element) => element !== dialog && !element.contains(dialog));
  const inertStates = background.map((element) => [element, element.inert]);
  background.forEach((element) => {
    element.inert = true;
  });
  const controls = () =>
    [
      ...dialog.querySelectorAll(
        'button, input, select, a[href], [tabindex="0"]',
      ),
    ].filter(
      (element) => !element.disabled && element.getClientRects().length > 0,
    );
  const keydown = (event) => {
    if (event.key === "Escape") {
      event.preventDefault();
      callback.invokeMethodAsync("DismissDialog").catch(() => {});
    }
    if (event.key !== "Tab") return;
    const all = controls();
    const first = all[0],
      last = all.at(-1);
    if (!dialog.contains(document.activeElement)) {
      event.preventDefault();
      (event.shiftKey ? last : first)?.focus();
      return;
    }
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last?.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first?.focus();
    }
  };
  document.addEventListener("keydown", keydown);
  controls()[0]?.focus();
  cleanup = () => {
    document.removeEventListener("keydown", keydown);
    inertStates.forEach(([element, value]) => {
      element.inert = value;
    });
    document.body.style.overflow = previousOverflow;
    if (previous?.isConnected) previous.focus();
  };
}
