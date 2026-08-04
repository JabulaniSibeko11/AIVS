document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-submission-tabs]").forEach(wrapper => {
        const tabs = Array.from(wrapper.querySelectorAll("[data-submission-tab]"));
        const panels = Array.from(wrapper.querySelectorAll("[data-submission-panel]"));

        if (!tabs.length || !panels.length) {
            return;
        }

        const activate = tab => {
            const targetId = tab.dataset.submissionTab;

            tabs.forEach(item => {
                const selected = item === tab;
                item.classList.toggle("active", selected);
                item.setAttribute("aria-selected", selected ? "true" : "false");
                item.tabIndex = selected ? 0 : -1;
            });

            panels.forEach(panel => {
                const selected = panel.id === targetId;
                panel.classList.toggle("active", selected);
                panel.hidden = !selected;
            });

            const url = new URL(window.location.href);
            url.searchParams.set("section", targetId.replace("submission-panel-", ""));
            window.history.replaceState({}, "", url);
        };

        tabs.forEach((tab, index) => {
            tab.addEventListener("click", () => activate(tab));

            tab.addEventListener("keydown", event => {
                if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) {
                    return;
                }

                event.preventDefault();

                let nextIndex = index;

                if (event.key === "ArrowRight") {
                    nextIndex = (index + 1) % tabs.length;
                } else if (event.key === "ArrowLeft") {
                    nextIndex = (index - 1 + tabs.length) % tabs.length;
                } else if (event.key === "Home") {
                    nextIndex = 0;
                } else if (event.key === "End") {
                    nextIndex = tabs.length - 1;
                }

                tabs[nextIndex].focus();
                activate(tabs[nextIndex]);
            });
        });

        const requestedSection = new URLSearchParams(window.location.search).get("section");
        const requestedTab = requestedSection
            ? tabs.find(tab => tab.dataset.submissionTab === `submission-panel-${requestedSection}`)
            : null;

        activate(requestedTab || tabs[0]);
    });
});
