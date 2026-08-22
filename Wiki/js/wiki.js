/* Wiki JavaScript — RagnaController v2.0.0 */
document.addEventListener('DOMContentLoaded', function() {
    // Language selector
    const langBtns = document.querySelectorAll('.lang-btn');
    const wikiMain = document.getElementById('wikiMain');
    const currentLang = localStorage.getItem('wikiLang') || 'en';
    
    // Set active language
    langBtns.forEach(btn => {
        const lang = btn.dataset.lang;
        if (lang === currentLang) {
            btn.classList.add('active');
        } else {
            btn.classList.remove('active');
        }
        
        btn.addEventListener('click', function() {
            // Remove active from all
            langBtns.forEach(b => b.classList.remove('active'));
            // Add active to clicked
            this.classList.add('active');
            localStorage.setItem('wikiLang', this.dataset.lang);
            
            // Switch content
            switchLang(this.dataset.lang);
        });
    });
    
    function switchLang(lang) {
        // Hide all sections except intro
        const sections = wikiMain.querySelectorAll('.wiki-section, #introduction, #getting-started, #installation, #core-features, #performance, #release, #tutorials, #faq');
        sections.forEach(section => {
            section.style.display = 'none';
        });
        
        // Show sections for this language (all are English, but structure maintained)
        const langSections = wikiMain.querySelectorAll('#' + lang + '-intro, #' + lang + '-getting-started, #' + lang + '-installation, #' + lang + '-core-features, #' + lang + '-performance, #' + lang + '-release, #' + lang + '-tutorials, #' + lang + '-faq');
        langSections.forEach(section => {
            section.style.display = 'block';
        });
    }
    
    // Initialize with saved language
    switchLang(currentLang);
    
    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });
    
    // Toggle details elements
    document.querySelectorAll('details').forEach(details => {
        details.addEventListener('toggle', function() {
            const content = this.querySelector('p');
            if (content) {
                content.style.display = this.open ? 'block' : 'none';
            }
        });
    });
    
    // Code block copy button creation
    document.querySelectorAll('pre code').forEach(codeBlock => {
        const pre = codeBlock.parentNode;
        const copyBtn = document.createElement('button');
        copyBtn.className = 'copy-btn';
        copyBtn.title = 'Copy code';
        copyBtn.innerHTML = '📋';
        copyBtn.style.cssText = 'position:absolute; right:8px; top:8px; background:var(--primary); color:white; border:none; border-radius:4px; padding:4px 8px; font-size:12px; cursor:pointer;';
        copyBtn.onclick = function() {
            navigator.clipboard.writeCode(codeBlock.textContent).then(() => {
                copyBtn.textContent = '✓';
                setTimeout(() => copyBtn.textContent = '📋', 1500);
            });
        };
        pre.style.position = 'relative';
        pre.appendChild(copyBtn);
    });
});