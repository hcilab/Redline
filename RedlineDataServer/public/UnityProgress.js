function UnityProgress(gameInstance, progress) {
    if (!gameInstance.Module)
        return;
    if (!gameInstance.progress) {
        gameInstance.progress = document.getElementById('loader');
        gameInstance.progressBars = gameInstance.progress.children[0].children;
        gameInstance.msg = document.createElement('p');
        gameInstance.msg.id = "msg";
        gameInstance.msg.textContent = "Loading Game";
        gameInstance.progress.appendChild(gameInstance.msg);
    }

    for( i = 0; i < progress * gameInstance.progressBars.length; i++ ) {
        gameInstance.progressBars[i].classList.add('loaded');
    }

    if (progress == 1)
        gameInstance.msg.textContent = "Setting up game";
}
