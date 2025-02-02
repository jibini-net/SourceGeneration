const dvdLogo = $("#dvdLogo");
const dvdContainer = $("#dvdContainer");

const speedScale = 60;

let x = 0;
let y = 0;

let vx = Math.random() * 0.5 + 0.25;
let vy = Math.sqrt(1 - vx * vx);

vx *= speedScale;
vy *= speedScale;

dvdLogo.click(() => {
    const randomPolarity = () => Math.random() >= 0.5 ? -1 : 1;

    const currAngle = Math.atan2(vy, vx);
    const angleOffset = Math.random() * Math.PI / 2 + Math.PI / 2;
    const newAngle = currAngle + angleOffset * randomPolarity();

    const specialSpeedScale = speedScale * (1 + Math.random() * 2);

    vx = Math.cos(newAngle) * specialSpeedScale;
    vy = Math.sin(newAngle) * specialSpeedScale;
});

function updateDvdLogo() {
    const time = Date.now() / 1000;
    const timeQuotient = 12;

    const r = (Math.sin(time / timeQuotient) + 1) / 2 * 255;
    const b = (Math.sin(time / timeQuotient + 2 * Math.PI / 3) + 1) / 2 * 255;
    const g = (Math.sin(time / timeQuotient + 4 * Math.PI / 3) + 1) / 2 * 255;

    dvdLogo.css({
        "background": `linear-gradient(0deg, rgba(${r}, ${g}, 255, 0.4) 0%, rgba(${r}, ${g}, ${b}, 0.2) 100%)`,
        "color": `rgba(${r}, ${g}, ${b}, 1)`,
        "box-shadow": `0 0 16px 16px rgba(${r}, ${g}, ${b}, 0.2)`
    });

    dvdContainer.css({
        "position": "relative",
        "width": "unset"
    });
    const relativeRect = dvdContainer[0].getBoundingClientRect();
    if (relativeRect.top < 0) {
        dvdContainer.css("position", "fixed");
        dvdContainer.css("width", `${relativeRect.width}px`);
    }

	const newX = x + (1.0 / 60) * vx;
    const newY = y + (1.0 / 60) * vy;

    const outerRect = dvdContainer[0].getBoundingClientRect();
    const innerRect = dvdLogo[0].getBoundingClientRect();

    if (newX <= 1 || newX + innerRect.width >= outerRect.width - 10) {
        vx = -vx;
    } else {
        x = newX;
    }

    if (newY <= 1 || newY + innerRect.height >= outerRect.height - 1) {
        vy = -vy;
    } else {
        y = newY;
    }

    x = Math.min(Math.max(x, 1), outerRect.width - innerRect.width - 10);
    y = Math.min(Math.max(y, 1), outerRect.height - innerRect.height - 1);

    dvdLogo.css({
        "left": `${x}px`,
        "top": `${y}px`
    });

    const bottomBarRect = document.getElementsByClassName("bottom-bar")[0].getBoundingClientRect();
    const maxHeight = bottomBarRect.top - outerRect.top;

    const opacityDistance = Math.max(0, window.innerHeight - outerRect.top);
    const opacity = Math.min(1, opacityDistance / 500) * 0.8;

    dvdContainer.css({
        "height": `${Math.min(Math.max(200, window.innerHeight - outerRect.top), maxHeight)}px`,
        "opacity": opacity
    });

	setTimeout(() => {
		requestAnimationFrame(updateDvdLogo);
	}, 1000 / 60);
}

requestAnimationFrame(updateDvdLogo);
