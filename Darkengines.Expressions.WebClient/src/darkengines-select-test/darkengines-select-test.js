import { LitElement, html } from '@polymer/lit-element/lit-element';
import '../darkengines-select/darkengines-select.js';
import '@polymer/paper-input/paper-input.js';
import '@polymer/paper-ripple/paper-ripple.js';
import { resolve, encode, decode } from '../json-ref';

function classes(classes) {
	return `${Object.keys(classes).filter(className => classes[className]).join(' ')}`;
}

class DarkenginesSelectTest extends LitElement {
	constructor() {
		super();
		this.text = 'mamadou';
		this.items = [];
	}
	textChanged(e) {
		this.text = e.detail.value;
	}
	itemsRequested(e) {
		return fetch('http://192.168.1.2:8080', {
			method: 'POST',
			headers: {
				'Accept': 'application/json',
				'Content-Type': 'application/json'
			},
			body: `Users.Where(u => u.DisplayName.StartsWith("${e.searchText}")).OrderBy(u => u.DisplayName).Skip(${e.index}).Take(${e.count})`
		}).then(response => {
			return response.json().then(json => {
				var decoded = decode(json);
				return decoded;
			});
		});
	}
	selectedItemTemplate(item) {
		return html`
			<img src="https://loremflickr.com/40/40?id=${item.Id}" class="avatar" />
			<div class="display-name">${item.DisplayName}</div>
		`;
	}
	emptySelectedItemTemplate() {
		return html`
			<div class="empty-avatar"></div>
			<div class="empty-display-name"></div>
		`;
	}
	itemTemplate(item, index, itemIndex, onClick) {
		return html`
			<img src="https://loremflickr.com/40/40?id=${item.Id}" class="avatar" />
			<div class="display-name">${item.DisplayName}</div>
		`;
	}
	selectStyle() {
		return html`<style>
	.selected-item,
	.item {
		width: 100%;
		padding: 8px;
		display: flex;
		align-items: center;
		position: relative;
	}

	.empty-display-name {
		margin-left: 8px;
		width: 100%;
		height: 12px;
		background-color: var(--google-grey-300);
	}
	.selected-item .empty-avatar {
		min-width: 40px;
		min-height: 40px;
		background-color: var(--google-grey-300);
	}

	.item img, .selected-item img {
		min-width: 40px;
		min-height: 40px;
		-webkit-animation: color-change-2x 0.5s linear infinite alternate both;
		animation: color-change-2x 0.5s linear infinite alternate both;
	}

	.item .display-name, .selected-item .display-name {
		margin-left: 8px;
	}

	@-webkit-keyframes color-change-2x {
		0% {
			background: var(--google-grey-100);
		}
		100% {
			background: var(--google-grey-300);
		}
	}

	@keyframes color-change-2x {
		0% {
			background: var(--google-grey-100);
		}
		100% {
			background: var(--google-grey-300);
		}
	}
</style>`
	}
	render() {
		return html`<style>
	:host {
		padding: 8px;
		max-width: 512px;
	}
</style>
<p>
I managed to solve most problems supporting overflow: auto and overflow: scroll on mobile Safari:

without hanging the scroll view after touching at the beginning of list, then moving down and then up (mobile Safari runs its default bouncing behavior for the whole page in that case)
support for fixed header / action bar on top without ugly overlapping of it by a scrollbar
Show code snippet

The only caveat I have is that when user touches and starts moving down and then up, nothing happens (expected: the list should scroll down). But at least the method prevents "pseudo scrolling down" and not confuses user.
I managed to solve most problems supporting overflow: auto and overflow: scroll on mobile Safari:

without hanging the scroll view after touching at the beginning of list, then moving down and then up (mobile Safari runs its default bouncing behavior for the whole page in that case)
support for fixed header / action bar on top without ugly overlapping of it by a scrollbar
Show code snippet

The only caveat I have is that when user touches and starts moving down and then up, nothing happens (expected: the list should scroll down). But at least the method prevents "pseudo scrolling down" and not confuses user.
I managed to solve most problems supporting overflow: auto and overflow: scroll on mobile Safari:

without hanging the scroll view after touching at the beginning of list, then moving down and then up (mobile Safari runs its default bouncing behavior for the whole page in that case)
support for fixed header / action bar on top without ugly overlapping of it by a scrollbar
Show code snippet

The only caveat I have is that when user touches and starts moving down and then up, nothing happens (expected: the list should scroll down). But at least the method prevents "pseudo scrolling down" and not confuses user.
</p>
<paper-input></paper-input>
<darkengines-select label="User" @search-value-changed="${this.searchValueChanged.bind(this)}" .itemTemplate="${this.itemTemplate.bind(this)}"
 .selectedItemTemplate="${this.selectedItemTemplate.bind(this)}" .items="${this.items}" .customStyle="${this.selectStyle}"
 .emptySelectedItemTemplate="${this.emptySelectedItemTemplate.bind(this)}" .itemsRequested="${this.itemsRequested.bind(this)}"
 .value="${this.value}">
</darkengines-select>
<darkengines-select label="User" @search-value-changed="${this.searchValueChanged.bind(this)}" .itemTemplate="${this.itemTemplate.bind(this)}"
 .selectedItemTemplate="${this.selectedItemTemplate.bind(this)}" .items="${this.items}" .customStyle="${this.selectStyle}"
 .emptySelectedItemTemplate="${this.emptySelectedItemTemplate.bind(this)}" .itemsRequested="${this.itemsRequested.bind(this)}"
 .value="${this.value}">
</darkengines-select>
<darkengines-select label="User" @search-value-changed="${this.searchValueChanged.bind(this)}" .itemTemplate="${this.itemTemplate.bind(this)}"
 .selectedItemTemplate="${this.selectedItemTemplate.bind(this)}" .items="${this.items}" .customStyle="${this.selectStyle}"
 .emptySelectedItemTemplate="${this.emptySelectedItemTemplate.bind(this)}" .itemsRequested="${this.itemsRequested.bind(this)}"
 .value="${this.value}">
</darkengines-select>
<p>
I managed to solve most problems supporting overflow: auto and overflow: scroll on mobile Safari:

without hanging the scroll view after touching at the beginning of list, then moving down and then up (mobile Safari runs its default bouncing behavior for the whole page in that case)
support for fixed header / action bar on top without ugly overlapping of it by a scrollbar
Show code snippet

The only caveat I have is that when user touches and starts moving down and then up, nothing happens (expected: the list should scroll down). But at least the method prevents "pseudo scrolling down" and not confuses user.
I managed to solve most problems supporting overflow: auto and overflow: scroll on mobile Safari:

without hanging the scroll view after touching at the beginning of list, then moving down and then up (mobile Safari runs its default bouncing behavior for the whole page in that case)
support for fixed header / action bar on top without ugly overlapping of it by a scrollbar
Show code snippet

The only caveat I have is that when user touches and starts moving down and then up, nothing happens (expected: the list should scroll down). But at least the method prevents "pseudo scrolling down" and not confuses user.
I managed to solve most problems supporting overflow: auto and overflow: scroll on mobile Safari:

without hanging the scroll view after touching at the beginning of list, then moving down and then up (mobile Safari runs its default bouncing behavior for the whole page in that case)
support for fixed header / action bar on top without ugly overlapping of it by a scrollbar
Show code snippet

The only caveat I have is that when user touches and starts moving down and then up, nothing happens (expected: the list should scroll down). But at least the method prevents "pseudo scrolling down" and not confuses user.
</p>`;
	}
	shouldUpdate(changedProperties) {
		console.log(changedProperties);
		return true;
	}
	itemClick(e, value) {
		this.value = value;
	}
	searchValueChanged(e) {
		fetch('http://192.168.1.2:8080', {
			method: 'POST',
			headers: {
				'Accept': 'application/json',
				'Content-Type': 'application/json'
			},
			body: `Users.Where(u => u.DisplayName.StartsWith("${e.detail.value}")).OrderBy(u => u.DisplayName).Skip(0).Take(1)`
		}).then(response => {
			return response.json().then(json => {
				var decoded = decode(json);
				this.items = decoded;
			});
		});
	}
	static get properties() {
		return {
			items: {
				type: Array,
				value: []
			},
			value: Object
		}
	}
}

window.customElements.define('darkengines-select-test', DarkenginesSelectTest);