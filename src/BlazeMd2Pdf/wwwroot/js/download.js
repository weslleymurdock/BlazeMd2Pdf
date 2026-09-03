window.blazeMd2Pdf = {
    getInnerHtml: async function (element) {
        if (!element) {
            return "";
        }

        await new Promise(requestAnimationFrame);
        await new Promise(requestAnimationFrame);
        return element.innerHTML || "";
    },
    downloadFile: function (fileName, contentType, content) {
        const blob = new Blob([content], { type: contentType });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(url);
    },
    downloadTextFile: function (fileName, content) {
        const blob = new Blob([content], { type: "text/markdown;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(url);
    }
};
