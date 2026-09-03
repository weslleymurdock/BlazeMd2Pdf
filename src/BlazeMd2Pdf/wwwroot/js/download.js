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
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    },

    downloadTextFile: function (fileName, content) {
        const blob = new Blob([content], { type: "text/markdown;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        anchor.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    },

    exportMarkdown: async function (element, format, fileName, options) {
        if (!element) {
            throw new Error("The Markdown export element is unavailable.");
        }

        await document.fonts.ready;
        await this.waitForImages(element);

        switch (format) {
            case "html":
                this.exportHtml(element, fileName, options);
                return;
            case "pdf":
                await this.exportPdf(element, fileName, options);
                return;
            case "png":
                await this.exportImage(element, fileName, options, "png");
                return;
            case "jpeg":
                await this.exportImage(element, fileName, options, "jpeg");
                return;
            default:
                throw new Error("Unsupported Markdown export format: " + format);
        }
    },

    waitForImages: async function (element) {
        const images = Array.from(element.querySelectorAll("img"));
        await Promise.all(images.map(function (image) {
            if (image.complete) {
                return Promise.resolve();
            }

            return new Promise(function (resolve) {
                image.addEventListener("load", resolve, { once: true });
                image.addEventListener("error", resolve, { once: true });
            });
        }));
    },

    exportHtml: function (element, fileName, options) {
        const clone = element.cloneNode(true);
        clone.removeAttribute("style");
        clone.style.width = "100%";
        clone.style.fontFamily = options.fontFamily;
        clone.style.fontSize = options.fontSize + "px";
        clone.style.lineHeight = options.lineHeight;
        clone.style.textAlign = this.getAlignment(options.alignment);

        const documentHtml = "<!DOCTYPE html>" +
            "<html lang=\"en\"><head><meta charset=\"utf-8\"><title>" +
            this.escapeHtml(fileName) +
            "</title><style>" +
            this.getExportCss(options) +
            "</style></head><body>" +
            clone.outerHTML +
            "</body></html>";

        this.downloadFile(fileName, "text/html;charset=utf-8", documentHtml);
    },

    exportPdf: async function (element, fileName, options) {
        if (typeof html2pdf !== "function") {
            throw new Error("html2pdf.js is not loaded. Check the export library reference in index.html.");
        }

        const pdfOptions = {
            margin: [options.marginTop, options.marginRight, options.marginBottom, options.marginLeft],
            filename: fileName,
            image: { type: "jpeg", quality: Math.min(Math.max(options.imageQuality, 0.1), 1) },
            enableLinks: true,
            pagebreak: {
                mode: ["css", "legacy"],
                avoid: ["img", "table", "pre", "blockquote"]
            },
            html2canvas: {
                scale: Math.min(Math.max(options.scale, 1), 4),
                useCORS: true,
                allowTaint: false,
                backgroundColor: "#ffffff"
            },
            jsPDF: {
                unit: "mm",
                format: options.pageFormat || "a4",
                orientation: options.orientation || "portrait",
                compressPDF: true
            }
        };

        await html2pdf().set(pdfOptions).from(element).save();
    },

    exportImage: async function (element, fileName, options, type) {
        if (typeof html2canvas !== "function") {
            throw new Error("html2canvas is not loaded. Check the export library reference in index.html.");
        }

        const canvas = await html2canvas(element, {
            scale: Math.min(Math.max(options.scale, 1), 4),
            useCORS: true,
            allowTaint: false,
            backgroundColor: "#ffffff",
            logging: false
        });

        const quality = Math.min(Math.max(options.imageQuality, 0.1), 1);
        const mime = type === "jpeg" ? "image/jpeg" : "image/png";
        const blob = await new Promise(function (resolve) {
            canvas.toBlob(resolve, mime, quality);
        });

        if (!blob) {
            throw new Error("The browser could not create the exported image.");
        }

        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        anchor.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    },

    getAlignment: function (alignment) {
        switch (alignment) {
            case 1:
                return "center";
            case 2:
                return "right";
            case 3:
                return "justify";
            default:
                return "left";
        }
    },

    getExportCss: function (options) {
        const alignment = this.getAlignment(options.alignment);
        const width = options.orientation === "landscape" ?
            (297 - options.marginLeft - options.marginRight) :
            (210 - options.marginLeft - options.marginRight);

        return "@page{size:" + options.pageFormat + " " + options.orientation + ";margin:" +
            options.marginTop + "mm " + options.marginRight + "mm " + options.marginBottom + "mm " + options.marginLeft + "mm;}" +
            "html,body{margin:0;padding:0;background:#fff;}" +
            "body{font-family:" + options.fontFamily + ";font-size:" + options.fontSize + "px;line-height:" + options.lineHeight + ";color:#1f2328;}" +
            ".markdown-export-document{width:" + Math.max(width, 1) + "mm;box-sizing:border-box;overflow-wrap:break-word;word-wrap:break-word;text-align:" + alignment + ";}" +
            ".markdown-export-document p{margin:0 0 1em;}" +
            ".markdown-export-document h1,.markdown-export-document h2,.markdown-export-document h3,.markdown-export-document h4,.markdown-export-document h5,.markdown-export-document h6{break-after:avoid;page-break-after:avoid;line-height:1.25;}" +
            ".markdown-export-document h1{padding-bottom:.3em;border-bottom:1px solid #d0d7de;}" +
            ".markdown-export-document img{max-width:100%;height:auto;}" +
            ".markdown-export-document table{width:100%;border-collapse:collapse;break-inside:avoid;page-break-inside:avoid;}" +
            ".markdown-export-document th,.markdown-export-document td{padding:6px 10px;border:1px solid #d0d7de;vertical-align:top;}" +
            ".markdown-export-document blockquote{margin:1em 0;padding:0 1em;border-left:4px solid #d0d7de;break-inside:avoid;page-break-inside:avoid;}" +
            ".markdown-export-document pre{padding:16px;overflow:auto;background:#f6f8fa;border-radius:6px;white-space:pre-wrap;overflow-wrap:anywhere;break-inside:avoid;page-break-inside:avoid;}" +
            ".markdown-export-document ul,.markdown-export-document ol{padding-left:2em;}" +
            ".markdown-export-document code{font-family:\"Cascadia Mono\",\"SFMono-Regular\",Consolas,\"Liberation Mono\",monospace;}";
    },

    escapeHtml: function (value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
};
