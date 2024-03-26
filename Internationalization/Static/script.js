// Internationalization : Stardew Valley Mod Translation Tool
const el = {};
document.addEventListener('DOMContentLoaded', ready);

/** Returns an array containing all elements matched by the given XPath expression. */
function $(a) {
	const res = document.evaluate(a,document,null,XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,null);
	return [...gen()];
	function* gen() {
		for (let i=0; i<res.snapshotLength; i++) yield res.snapshotItem(i);
	}
}

/** Create a new element */
function node(nodeType, pars){
	var e = document.createElement(nodeType);
	for (var p in pars) {
		if (p == "text") e.textContent = pars[p];
		else e.setAttribute(p,pars[p]);
	}
	return e;
}

function as_text(res) {return res.text()}
function as_json(res) {return res.json()}

/** Initialize the web app */
function ready() {
	// Map id to their html element
	for(let e of $("//*[@id]")) el[e.id.replaceAll("-","_")] = e;

	// Register events
	el.current.addEventListener('click', select_ingame_locale);
	el.locale.addEventListener('change', update_locale);
	el.mod.addEventListener('change', update_mod);

	// Load data
	populate_mods();
}

/** Request the mod list and populate drop down box */
async function populate_mods() {
	// Request mod list
	const mods = await fetch("/mods").then(as_json);

	// Populate drop down box.
	const mod_list = Object.keys(mods).sort(mod_cmp);
	const mod_options = mod_list.map((id) => node("option", {value:id, text:mods[id]}));
	el.mod.replaceChildren(...mod_options);

	// Select last mod
	el.mod.value = localStorage.getItem("modid");
	update_mod();

	function mod_cmp(a,b) {
		if (mods[a] < mods[b]) return -1;
		if (mods[a] > mods[b]) return  1;
		return 0;
	}
}

/** Load the mod's translation file into the editor */
async function update_mod() {
	const modid = el.mod.value;
	localStorage.setItem("modid", modid);

	// Request mod locale info
	const info = await fetch("/mods/" + modid).then(as_json);
	if (el.locale.value === "") {
		el.locale.value = info.current_locale;
	}

	// Populate locale suggestions
	const locale_list = info.locales.sort();
	const locale_options = locale_list.map((id) => node("option", {value:id, text:id}));
	el.locale_list.replaceChildren(...locale_options);
	
	// Generate the translation editor for this mod
	const text_new = await fetch("/file/" + modid + "/default").then(as_text);
	el.new.textContent = text_new;
	// text.split(/"((?:\\\\|\\"|[^"])+)"\s*:\s*"((?:\\\\|\\"|[^"])+)"|(\/\/.*)|(\/\*(?:[^*]|\*[^/])*\*\/)/i)
	
	// Load the selected locale
	update_locale();
}

/** Set editor locale to what the current mod is set to in-game. */
async function select_ingame_locale() {
	const info = await fetch("/mods/" + el.mod.value).then(as_json);
	el.locale.value = info.current_locale;
	update_locale();
}

/** Load the selected locale into the editor. */
async function update_locale() {
	const text_old = await fetch("/file/" + el.mod.value + "/" + el.locale.value).then(as_text)
	el.old.textContent = text_old;
}
