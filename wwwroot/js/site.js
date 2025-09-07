// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Email obfuscation function
function setEmail(over) {
    const emailLink = document.getElementById("emailLink");
    if (emailLink) {
        if (over) {
            // Get the encoded email from the data attribute or config
            var enc = emailLink.getAttribute("data-email") || "YWQ55dmVQsZHRAZ21QhaWwQuY29t";
            enc = enc.replaceAll("Q", "");
            emailLink.setAttribute("href", "mai" + "lto:".concat(atob(enc)));
        } else {
            emailLink.setAttribute("href", "#");
        }
    }
}

// Footer email obfuscation function
function setFooterEmail(over) {
    const emailLink = document.getElementById("footerEmailLink");
    if (emailLink) {
        if (over) {
            // Get the encoded email from the data attribute or config
            var enc = emailLink.getAttribute("data-email") || "YWQ55dmVQsZHRAZ21QhaWwQuY29t";
            enc = enc.replaceAll("Q", "");
            emailLink.setAttribute("href", "mai" + "lto:".concat(atob(enc)));
        } else {
            emailLink.setAttribute("href", "#");
        }
    }
}
