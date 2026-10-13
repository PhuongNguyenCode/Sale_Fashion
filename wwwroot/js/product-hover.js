(function () {
  "use strict";

  window.FashionStoreProducts = {
    initProductHover: function () {
      document.querySelectorAll(".product-card").forEach(function (card) {
        var hoverSrc = card.getAttribute("data-hover-src");
        if (!hoverSrc) return;
        if (card.querySelector(".card-img-hover")) return;
        var img = card.querySelector(".card-img-top");
        if (!img) return;
        var second = document.createElement("img");
        second.src = hoverSrc;
        second.alt = "";
        second.className = "card-img-hover";
        second.loading = "lazy";
        img.parentNode.appendChild(second);
        card.classList.add("has-hover");
      });
    }
  };
})();
