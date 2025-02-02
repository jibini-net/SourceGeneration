const vcrLogo = $("#vcrLogo");
const vcrContainer = $("#vcrContainer");

const speedScale = 120;

let x = 0;
let y = 0;

let vx = Math.random() * 0.5 + 0.25;
let vy = Math.sqrt(1 - vx * vx);

vx *= speedScale;
vy *= speedScale;

vcrLogo.click(() => {
    const randomPolarity = () => Math.random() >= 0.5 ? -1 : 1;

    const currAngle = Math.atan2(vy, vx);
    const angleOffset = Math.random() * Math.PI / 2 + Math.PI / 2;
    const newAngle = currAngle + angleOffset * randomPolarity();

    vx = Math.cos(newAngle) * speedScale;
    vy = Math.sin(newAngle) * speedScale;
});

function updateLogoColor() {
	const time = Date.now() / 1000;

	const r = (Math.sin(time) + 1) / 2 * 255;
	const b = (Math.sin(time + 2 * Math.PI / 3) + 1) / 2 * 255;
	const g = (Math.sin(time + 4 * Math.PI / 3) + 1) / 2 * 255;

    vcrLogo.css("background", `linear-gradient(0deg, rgb(${r}, ${g}, ${b}) 0%, rgb(${r}, ${g}, 255) 100%), rgb(${r}, ${g}, ${b})`);
    vcrLogo.css("box-shadow", `0 0 16px 16px rgba(${r}, ${g}, ${b}, 0.5)`);

    vcrContainer.css("position", "relative");
    vcrContainer.css("width", "unset");

    const relativeRect = vcrContainer[0].getBoundingClientRect();
    if (relativeRect.top < 0) {
        vcrContainer.css("position", "fixed");
        vcrContainer.css("width", `${relativeRect.width}px`);
    }

	const newX = x + (1.0 / 60) * vx;
    const newY = y + (1.0 / 60) * vy;

    const outerRect = vcrContainer[0].getBoundingClientRect();
    const innerRect = vcrLogo[0].getBoundingClientRect();

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

    vcrLogo.css("left", `${x}px`);
    vcrLogo.css("top", `${y}px`);

    const bottomBarRect = document.getElementsByClassName("bottom-bar")[0].getBoundingClientRect();
    const maxHeight = bottomBarRect.top - outerRect.top;

    vcrContainer.css("height", `${Math.min(Math.max(1, window.innerHeight - outerRect.top), maxHeight)}px`);

	setTimeout(() => {
		requestAnimationFrame(updateLogoColor);
	}, 1000 / 60);
}

requestAnimationFrame(updateLogoColor);
