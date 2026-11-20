const toggle = document.querySelector(".language");
const translations = [...document.querySelectorAll("[data-en]")].map(element => ({
  element,
  chinese: element.innerHTML,
  english: element.dataset.en
}));

function displayLanguage(language) {
  const english = language === "en";
  document.documentElement.lang = english ? "en" : "zh-CN";
  document.title = english ? "File Occupied Release" : "文件占用释放 · File Occupied Release";
  for (const entry of translations) {
    if (english) entry.element.textContent = entry.english;
    else entry.element.innerHTML = entry.chinese;
  }
  toggle.textContent = english ? "简体中文" : "English";
  toggle.lang = english ? "zh-CN" : "en";
}

const initial = new URLSearchParams(location.search).get("lang");
displayLanguage(initial || (navigator.language.toLowerCase().startsWith("zh") ? "zh" : "en"));
toggle.hidden = false;
toggle.addEventListener("click", () => {
  const language = document.documentElement.lang === "en" ? "zh" : "en";
  const url = new URL(location.href);
  url.searchParams.set("lang", language);
  history.replaceState(null, "", url);
  displayLanguage(language);
});
