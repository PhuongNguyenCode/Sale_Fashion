(function () {
  "use strict";

  function prefersReducedMotion() {
    return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  }

  function isDesktop() {
    return window.matchMedia("(min-width: 1025px) and (hover: hover)").matches;
  }

  window.FashionStoreMotion = {
    initHeroAnimation: function () {
      var hero = document.querySelector(".hero");
      var showNav = function () {
        document.querySelectorAll(".page-home .nav-logo, .page-home .nav-menu, .page-home .nav-actions").forEach(function (el) {
          el.style.opacity = "1";
        });
      };
      if (!hero) {
        showNav();
        return;
      }
      if (typeof gsap === "undefined") {
        showNav();
        var visual0 = hero.querySelector(".hero-visual");
        if (visual0) visual0.classList.add("is-ready");
        return;
      }

      if (prefersReducedMotion()) {
        document.querySelectorAll(".page-home .nav-logo, .page-home .nav-menu, .page-home .nav-actions").forEach(function (el) {
          el.style.opacity = "1";
        });
        var visual = hero.querySelector(".hero-visual");
        if (visual) visual.classList.add("is-ready");
        return;
      }

      var tl = gsap.timeline({ defaults: { ease: "power3.out" } });
      tl.to(".page-home .nav-logo", { opacity: 1, duration: 0.8, delay: 0.1 })
        .to(".page-home .nav-menu, .page-home .nav-actions", { opacity: 1, duration: 0.7 }, "-=0.4");

      var visual = hero.querySelector(".hero-visual");
      if (visual) {
        tl.add(function () { visual.classList.add("is-ready"); }, "-=0.2");
        tl.fromTo(hero.querySelector(".hero-visual img"), { scale: 1.12 }, { scale: 1, duration: 1.4 }, "<");
      }

      tl.from(".hero-title .line span", { yPercent: 110, duration: 1.05, stagger: 0.12 }, "-=0.8")
        .from(".hero-kicker, .hero-cta", { opacity: 0, y: 24, duration: 0.7, stagger: 0.08 }, "-=0.55")
        .from(".hero-meta, .scroll-indicator", { opacity: 0, duration: 0.6 }, "-=0.3");
    },

    initScrollReveal: function () {
      var revealItems = document.querySelectorAll(
        "[data-reveal], [data-clip], .category-row, .product-card, .bag-item, .pdp-info, .product-gallery, .cart-summary, .checkout-layout, .auth-card, .site-footer .footer-grid, .site-footer .footer-bottom"
      );
      if (!revealItems.length || prefersReducedMotion() || !("IntersectionObserver" in window)) return;

      var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-in");
          observer.unobserve(entry.target);
        });
      }, { rootMargin: "0px 0px -7% 0px", threshold: 0.01 });

      revealItems.forEach(function (el) { observer.observe(el); });
      document.body.classList.add("motion-ready");

      // Anchor jumps and fast wheel/touch scrolls can move past an element
      // between observer updates. Reveal visible/passed items on the next frame.
      var revealCheckQueued = false;
      function revealVisibleOrPassed() {
        revealCheckQueued = false;
        revealItems.forEach(function (el) {
          if (el.classList.contains("is-in")) return;
          var rect = el.getBoundingClientRect();
          if (rect.bottom < 0 || (rect.top < window.innerHeight && rect.bottom > 0)) {
            el.classList.add("is-in");
            observer.unobserve(el);
          }
        });
      }
      function scheduleRevealCheck() {
        if (revealCheckQueued) return;
        revealCheckQueued = true;
        window.requestAnimationFrame(revealVisibleOrPassed);
      }
      window.addEventListener("scroll", scheduleRevealCheck, { passive: true });
      window.addEventListener("hashchange", scheduleRevealCheck);
      scheduleRevealCheck();
    },

    initParallax: function () {
      // Keep editorial imagery stable; the restrained hero scale provides depth without scroll work.
    },

    initMarquee: function (lenis) {
      var track = document.querySelector(".marquee-track");
      if (!track) return;
      var toggle = document.querySelector(".marquee-toggle");

      var clone = track.innerHTML;
      track.innerHTML = clone + clone;

      if (prefersReducedMotion() || typeof gsap === "undefined") {
        if (toggle) toggle.hidden = true;
        return;
      }

      var tween = gsap.to(track, {
        xPercent: -50,
        duration: 28,
        ease: "none",
        repeat: -1
      });

      if (toggle) {
        toggle.addEventListener("click", function () {
          var paused = !tween.paused();
          tween.paused(paused);
          toggle.setAttribute("aria-pressed", String(paused));
          toggle.setAttribute("aria-label", paused ? "Tiếp tục chuyển động" : "Tạm dừng chuyển động");
          toggle.title = paused ? "Tiếp tục chuyển động" : "Tạm dừng chuyển động";
          var icon = toggle.querySelector("i");
          if (icon) icon.className = paused ? "fas fa-play" : "fas fa-pause";
        });
      }

      if (lenis) return;
    },

    initHorizontalScroll: function () {
      var section = document.querySelector(".lookbook");
      var track = document.querySelector(".lookbook-track");
      if (!section || !track) return;
      if (prefersReducedMotion() || typeof gsap === "undefined" || typeof ScrollTrigger === "undefined") return;
      if (!window.matchMedia("(min-width: 1025px)").matches) return;

      gsap.registerPlugin(ScrollTrigger);
      var distance = track.scrollWidth - window.innerWidth + 80;

      gsap.to(track, {
        x: function () { return -distance; },
        ease: "none",
        scrollTrigger: {
          trigger: section,
          start: "top top",
          end: function () { return "+=" + distance; },
          scrub: true,
          pin: true,
          anticipatePin: 1
        }
      });
    },

    initCursor: function () {
      var cursor = document.querySelector(".cursor");
      if (!cursor || !isDesktop() || prefersReducedMotion()) return;

      document.body.classList.add("has-custom-cursor");
      cursor.style.display = "block";
      var label = cursor.querySelector(".cursor-label");
      var x = window.innerWidth / 2;
      var y = window.innerHeight / 2;
      var cx = x;
      var cy = y;

      window.addEventListener("mousemove", function (e) {
        x = e.clientX;
        y = e.clientY;
      });

      function loop() {
        cx += (x - cx) * 0.18;
        cy += (y - cy) * 0.18;
        cursor.style.transform = "translate(" + cx + "px, " + cy + "px)";
        requestAnimationFrame(loop);
      }
      loop();

      document.querySelectorAll("[data-cursor], .product-card, .hero-visual, .fs-btn, .btn-primary-custom").forEach(function (el) {
        el.addEventListener("mouseenter", function () {
          var text = el.getAttribute("data-cursor");
          if (el.classList.contains("product-card")) text = text || "View";
          if (el.classList.contains("hero-visual")) text = text || "Explore";
          if (el.classList.contains("fs-btn") || el.classList.contains("btn-primary-custom")) text = text || "→";
          if (label) {
            label.textContent = text || "";
            label.style.opacity = text ? "1" : "0";
          }
        });
        el.addEventListener("mouseleave", function () {
          if (label) label.style.opacity = "0";
        });
      });
    },

    initPageTransition: function () {
      // Avoid delaying the first interactive paint on transactional pages.
    }
  };
})();
