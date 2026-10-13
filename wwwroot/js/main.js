(function () {
  "use strict";

  document.addEventListener("DOMContentLoaded", function () {
    if (window.FashionStoreNav) {
      window.FashionStoreNav.init();
    }

    var reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var lenis = null;

    if (!reduce && typeof Lenis !== "undefined") {
      lenis = new Lenis({
        duration: 1.1,
        smoothWheel: true
      });

      if (typeof gsap !== "undefined" && typeof ScrollTrigger !== "undefined") {
        lenis.on("scroll", ScrollTrigger.update);
        gsap.ticker.add(function (time) {
          lenis.raf(time * 1000);
        });
        gsap.ticker.lagSmoothing(0);
      } else {
        function raf(time) {
          lenis.raf(time);
          requestAnimationFrame(raf);
        }
        requestAnimationFrame(raf);
      }
    }

    var motion = window.FashionStoreMotion;
    if (!motion) return;

    motion.initPageTransition();
    motion.initHeroAnimation();
    motion.initScrollReveal();
    motion.initParallax();
    motion.initMarquee(lenis);
    motion.initHorizontalScroll();
    motion.initCursor();
  });
})();
