(function () {
  "use strict";

  window.FashionStoreNav = {
    init: function () {
      var header = document.getElementById("siteHeader");
      var toggle = document.getElementById("menuToggle");
      var menu = document.getElementById("mobileMenu");
      var searchToggle = document.getElementById("searchToggle");
      var searchOverlay = document.getElementById("searchOverlay");
      var searchClose = document.getElementById("searchClose");
      var searchInput = document.getElementById("siteSearch");

      function onScroll() {
        if (!header) return;
        header.classList.toggle("is-scrolled", window.scrollY > 24);
      }

      onScroll();
      window.addEventListener("scroll", onScroll, { passive: true });

      if (toggle && menu) {
        toggle.addEventListener("click", function () {
          var open = menu.hidden;
          menu.hidden = !open;
          menu.classList.toggle("is-open", open);
          toggle.classList.toggle("is-open", open);
          toggle.setAttribute("aria-expanded", String(open));
          document.body.style.overflow = open ? "hidden" : "";
        });

        menu.querySelectorAll("a").forEach(function (link) {
          link.addEventListener("click", function () {
            menu.hidden = true;
            menu.classList.remove("is-open");
            toggle.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
            document.body.style.overflow = "";
          });
        });
      }

      function openSearch() {
        if (!searchOverlay) return;
        searchOverlay.hidden = false;
        document.body.style.overflow = "hidden";
        if (searchInput) searchInput.focus();
      }

      function closeSearch() {
        if (!searchOverlay) return;
        searchOverlay.hidden = true;
        document.body.style.overflow = "";
      }

      if (searchToggle) searchToggle.addEventListener("click", openSearch);
      if (searchClose) searchClose.addEventListener("click", closeSearch);
      if (searchOverlay) {
        searchOverlay.addEventListener("click", function (e) {
          if (e.target === searchOverlay) closeSearch();
        });
      }

      document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
          closeSearch();
          if (menu && !menu.hidden) toggle.click();
        }
      });
    }
  };
})();
