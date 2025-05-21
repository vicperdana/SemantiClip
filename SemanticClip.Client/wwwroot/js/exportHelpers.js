// Export Helpers for SemantiClip

window.exportHelpers = {
    saveAsFile: function (fileName, byteBase64) {
        const link = document.createElement('a');
        link.download = fileName;
        link.href = "data:application/octet-stream;base64," + byteBase64;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },
    
    saveAsTextFile: function (fileName, content) {
        const blob = new Blob([content], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        
        const link = document.createElement('a');
        link.download = fileName;
        link.href = url;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },
    
    saveAsHtml: function (fileName, content) {
        // Basic HTML wrapper around the content
        const htmlContent = `<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>${fileName}</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; margin: 20px; }
        h1, h2, h3 { color: #333; }
        pre { background-color: #f5f5f5; padding: 10px; border-radius: 5px; }
    </style>
</head>
<body>
    ${content}
</body>
</html>`;
        
        const blob = new Blob([htmlContent], { type: 'text/html' });
        const url = URL.createObjectURL(blob);
        
        const link = document.createElement('a');
        link.download = fileName;
        link.href = url;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};