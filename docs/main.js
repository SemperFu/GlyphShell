window.addEventListener('load', function() {
  AsciinemaPlayer.create('demo/demo.cast', document.getElementById('player'), {
    theme: 'wt-dark',
    fit: 'width',
    autoPlay: true,
    loop: true,
    poster: 'npt:0:5',
    idleTimeLimit: 3,
    terminalFontFamily: "'Cascadia Code NF', 'CaskaydiaCove Nerd Font', 'JetBrains Mono', monospace"
  });
});

function copyInstall(btn) {
  var text = 'Install-Module GlyphShell -Scope CurrentUser\nImport-Module GlyphShell';
  navigator.clipboard.writeText(text).then(function() {
    btn.textContent = 'Copied!';
    btn.classList.add('copied');
    setTimeout(function() { btn.textContent = 'Copy'; btn.classList.remove('copied'); }, 2000);
  });
}

