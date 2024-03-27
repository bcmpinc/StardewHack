// Internationalization : Stardew Valley Mod Translation Tool
const el = {};
document.addEventListener('DOMContentLoaded', ready);

// detect if contentEditable="plaintext-only" is supported.
const has_plaintext_only = (function(){
	try {
		node("p").contentEditable = "plaintext-only";
		return true;
	} catch {}
	return false;
})();

// Translation json parser.
const magic = new RegExp([
	/(?:(?<key1>[_a-z][_a-z0-9]*)|"(?<key2>.*?)(?<!\\(?:\\\\)+)")(?<colon>\s*:\s*)"(?<value>.*?)(?<!\\(?:\\\\)+)"/, // entry
	/\/\/(?<sc>.*)/,      // Single line comment
	/\/\*(?<mc>[^]*?)\*\//, // Multiline comment
].map((x)=>x.source).join('|'), "dgiu");

/** Returns an array containing all elements matched by the given XPath expression. */
function $(a, root) {
	const res = document.evaluate(a,root ?? document,null,XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,null);
	return [...gen()];
	function* gen() {
		for (let i=0; i<res.snapshotLength; i++) yield res.snapshotItem(i);
	}
}

/** Create a new element */
function node(nodeType, pars){
	var e = document.createElement(nodeType);
	for (var p in pars) {
		if (p=="text") e.appendChild(text(pars[p]));
		else e.setAttribute(p,pars[p]);
	}
	return e;
}

/** Create a text node */
function text(content) {
	return document.createTextNode(content);
}

function as_text(res) {return res.text()}
function as_json(res) {return res.json()}

function textarea_fit(e) {
	e.style.height = "1lh";
	e.style.height = (e.scrollHeight-4)+"px";
}

/** Initialize the web app */
function ready() {
	// Map id to their html element
	for(let e of $("//*[@id]")) el[e.id.replaceAll("-","_")] = e;

	// Register events
	el.current.addEventListener('click', select_ingame_locale);
	el.locale.addEventListener('change', update_locale);
	el.mod.addEventListener('change', update_mod);
	
	window.addEventListener("resize", () => {
		for (let x of $("//textarea")) textarea_fit(x);
	});

	// Load data
	el.locale.value = localStorage.getItem("locale") ?? "";
	populate_mods();
}

/** Request the mod list and populate drop down box */
async function populate_mods() {
	// Request mod list
	const mods = await fetch("/mods").then(as_json);

	// Populate drop down box.
	const mod_list = Object.keys(mods).sort(mod_cmp);
	const mod_options = mod_list.map((id) => node("option", {value:id, text: mods[id]}));
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
	el.new.replaceChildren(...generate_editor(text_new));
	el.new.dataset.raw = text_new;
	
	// Load the selected locale
	update_locale();
}

function* generate_editor(content, readonly) {
	let pos = 0;
	for (let m of content.matchAll(magic)) {
		let g = m.groups;
		if (g.sc) yield node("div", {'class': "comment", text: g.sc});
		if (g.mc) yield node("div", {'class': "comment", text: g.mc});
		if (g.key1 || g.key2) {
			let r = node("div", {'class': "entry"});
			let key = g.key1 ?? g.key2;
			if (readonly) {
				let field;
				r.replaceChildren(
					node("span", {'class': "key", text: key}),
					node("span", {'class': "default", "data-key": key}),
					field = node("textarea", {'class': "value", text: g.value, readonly:""}),
				);
			} else {
				let field;
				r.replaceChildren(
					node("span", {'class': "key", text: key}),
					node("span", {'class': "default", text: g.value}),
					field = node("textarea", {'class': "value", "data-key": key, "data-position":m.indices.groups.value}),
				);
				field.addEventListener('input', (e)=>textarea_fit(e.target));
			}
			yield r;
		}
		pos = m.index + m[0].length;
	}
}

/** Set editor locale to what the current mod is set to in-game. */
async function select_ingame_locale() {
	const info = await fetch("/mods/" + el.mod.value).then(as_json);
	el.locale.value = info.current_locale;
	update_locale();
}

/** Load the selected locale into the editor. */
async function update_locale() {
	const modid = el.mod.value;
	const locale = el.locale.value;
	localStorage.setItem("locale", locale);

	// Load current translation from game
	fetch("/lang/" + el.mod.value + "/" + locale).then(as_json).then(
	(lang) => {
		for (let e of $('.//*[@data-key]', el.new)) {
			e.replaceChildren(text(lang[e.dataset.key] ?? ""));
			textarea_fit(e);
		}
	});
	
	// Generate old translation contents
	fetch("/file/" + modid + "/" + locale).then(as_text).then(
	(text_old) => {
		el.old.replaceChildren(...generate_editor(text_old, true));
		for (let x of $(".//textarea", el.old)) textarea_fit(x);
		fetch("/lang/" + el.mod.value + "/default").then(as_json).then(
		(lang) => {
			for (let e of $('.//*[@data-key]', el.old)) {
				e.replaceChildren(text(lang[e.dataset.key] ?? ""));
			}
		});	
	});
}
