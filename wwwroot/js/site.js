// Hospital System - UI interactions
(function () {
    "use strict";

    // ---- Theme ----
    const root = document.documentElement;
    const savedTheme = localStorage.getItem("hs-theme") || "light";
    root.setAttribute("data-theme", savedTheme);

    window.toggleTheme = function () {
        const current = root.getAttribute("data-theme");
        const next = current === "dark" ? "light" : "dark";
        root.setAttribute("data-theme", next);
        localStorage.setItem("hs-theme", next);
        updateThemeIcon();
    };

    function updateThemeIcon() {
        const icon = document.getElementById("theme-icon");
        if (!icon) return;
        const dark = root.getAttribute("data-theme") === "dark";
        icon.className = dark ? "fa-solid fa-sun" : "fa-solid fa-moon";
    }

    // ---- Sidebar ----
    window.toggleSidebar = function () {
        if (window.innerWidth <= 768) {
            document.body.classList.toggle("sidebar-open");
        } else {
            document.body.classList.toggle("sidebar-collapsed");
            localStorage.setItem("hs-sidebar", document.body.classList.contains("sidebar-collapsed") ? "1" : "0");
        }
    };

    // ---- Dropdowns ----
    window.toggleDropdown = function (id, ev) {
        if (ev) ev.stopPropagation();
        document.querySelectorAll(".dropdown-menu-custom.show").forEach(function (m) {
            if (m.id !== id) m.classList.remove("show");
        });
        const menu = document.getElementById(id);
        if (menu) menu.classList.toggle("show");
    };

    document.addEventListener("click", function () {
        document.querySelectorAll(".dropdown-menu-custom.show").forEach(function (m) {
            m.classList.remove("show");
        });
    });

    // ---- Toast ----
    window.showToast = function (message, type) {
        let host = document.querySelector(".toast-host");
        if (!host) {
            host = document.createElement("div");
            host.className = "toast-host";
            document.body.appendChild(host);
        }
        const card = document.createElement("div");
        card.className = "toast-card " + (type || "");
        const icon = type === "success" ? "fa-circle-check" : type === "error" ? "fa-circle-exclamation" : "fa-circle-info";
        card.innerHTML = '<i class="fa-solid ' + icon + ' t-ico"></i><div>' + message + "</div>";
        host.appendChild(card);
        setTimeout(function () {
            card.style.opacity = "0";
            card.style.transition = "opacity .3s";
            setTimeout(function () { card.remove(); }, 300);
        }, 4000);
    };

    // ---- Modal ----
    window.openModal = function (id) {
        const m = document.getElementById(id);
        if (m) m.classList.add("show");
    };
    window.closeModal = function (id) {
        const m = document.getElementById(id);
        if (m) m.classList.remove("show");
    };
    document.addEventListener("click", function (e) {
        if (e.target.classList && e.target.classList.contains("modal-overlay")) {
            e.target.classList.remove("show");
        }
    });

    // ---- Password toggle ----
    window.togglePassword = function (btn) {
        const input = btn.parentElement.querySelector("input");
        if (!input) return;
        const show = input.type === "password";
        input.type = show ? "text" : "password";
        btn.querySelector("i").className = show ? "fa-solid fa-eye-slash" : "fa-solid fa-eye";
    };

    // ---- Init ----
    document.addEventListener("DOMContentLoaded", function () {
        updateThemeIcon();
        if (localStorage.getItem("hs-sidebar") === "1" && window.innerWidth > 768) {
            document.body.classList.add("sidebar-collapsed");
        }
        // Flash server toasts
        const flash = document.getElementById("flash-data");
        if (flash) {
            if (flash.dataset.success) showToast(flash.dataset.success, "success");
            if (flash.dataset.error) showToast(flash.dataset.error, "error");
        }
    });
})();
