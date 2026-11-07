window.relatorios = {
    baixar: async function (url) {
        const response = await fetch(url, { credentials: "same-origin", cache: "no-store", redirect: "follow" });
        if (response.redirected) return { status: 401, code: "" };
        if (!response.ok) {
            let code = "";
            try { code = (await response.json()).code || ""; } catch { }
            return { status: response.status, code: code };
        }
        if (!(response.headers.get("Content-Type") || "").startsWith("text/csv")) return { status: 503, code: "" };
        const blob = await response.blob();
        const link = document.createElement("a"), objectUrl = URL.createObjectURL(blob);
        link.href = objectUrl; link.download = "relatorio-" + url.split("?")[0].split("/").pop() + ".csv";
        document.body.appendChild(link); link.click(); link.remove();
        setTimeout(() => URL.revokeObjectURL(objectUrl), 10000);
        return { status: 200, code: "" };
    }
};
