const LS = localStorage;
document.addEventListener('DOMContentLoaded', ready);

function $(a){return document.getElementById(a);}

function current_mod() { return $("mod").value; }
function current_locale() { return $("locale").value; }

async function ready() {
	const res = await fetch("/mods");
	const mods = await res.json();
	const mod = $("mod");
	mod.replaceChildren(...Object.keys(mods).sort(mod_cmp).map((id) => node("option", {value:id, text:mods[id]})));
	mod.addEventListener('change', update_mod);
	mod.value = LS.getItem("modid");
	
	$("current").addEventListener('click', to_current_locale);
	$("locale").addEventListener('change', update_locale);

	update_mod();
	
	function mod_cmp(a,b) {
		if (mods[a] < mods[b]) return -1;
		if (mods[a] > mods[b]) return  1;
		return 0;
	}
}

async function to_current_locale() {
	const res = await fetch("/mods/" + current_mod());
	const info = await res.json();
	$("locale").value = info.current_locale;
	update_locale();
}

async function update_mod() {
	const modid = current_mod();
	LS.setItem("modid", modid);

	const res = await fetch("/mods/" + modid);
	const info = await res.json();
	const locale = $("locale");
	if (locale.value === "") {
		locale.value = info.current_locale;
	}

	$("locale-list").replaceChildren(...info.locales.sort().map((id) => node("option", {value:id, text:id})));
	
	fetch("/file/" + modid + "/default").then((res) => res.text()).then((res) => $("new").textContent = res);
	update_locale();
}

function update_locale() {
	fetch("/file/" + current_mod() + "/" + current_locale()).then((res) => res.text()).then((res) => $("old").textContent = res);
}

// Create a new element
// Don't use style in pars. Gives 'component not available' exception.
function node(nodeType, pars){
	var e = document.createElement(nodeType);
	for (var p in pars) {
		if (p == "text") e.textContent = pars[p];
		else e.setAttribute(p,pars[p]);
	}
	return e;
}
