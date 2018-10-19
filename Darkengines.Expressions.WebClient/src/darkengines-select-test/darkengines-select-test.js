import { LitElement, html } from '@polymer/lit-element/lit-element';
import '../darkengines-select/darkengines-select.js';
import '@polymer/paper-input/paper-input.js';
import { resolve, encode, decode } from '../json-ref';

class DarkenginesSelectTest extends LitElement {
	constructor() {
		super();
		this.text = 'mamadou';
		this.items = [];
	}
	textChanged(e) {
		console.log(e);
		this.text = e.detail.value;
	}
	itemsRequested(e) {
		fetch('https://localhost:8080', {
			method: 'POST',
			headers: {
				'Accept': 'application/json',
				'Content-Type': 'application/json'
			},
			body: `Users.Where(u => u.DisplayName.StartsWith("${e.detail.searchText}")).OrderBy(u => u.DisplayName).Skip(${e.detail.index}).Take(${e.detail.count})`
		}).then(response => {
			return response.json().then(json => {
				var decoded = decode(json);
				this.items = [...this.items, ...decoded];
			});
		});
	}
	itemTemplate(item, index, itemIndex) {
		return html`<div class="item ${item === this.value ? 'selected' : ''} ${index === itemIndex ? 'hover' : ''}" @click="${e => this.itemClick(e, item)}">
	<img src="https://loremflickr.com/40/40?id=${item.Id}" class="avatar" />
	<div class="display-name">${item.DisplayName}</div>
</div>`;
	}
	selectStyle() {
		return html`<style>
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

	.item {
		display: flex;
		align-items: center;
	}

	.item img {
		min-width: 40px;
		min-height: 40px;
		-webkit-animation: color-change-2x 0.5s linear infinite alternate both;
	    animation: color-change-2x 0.5s linear infinite alternate both;
	}

	.items .item {
		padding: 8px;
	}
	.items .item.hover {
		background-color: var(--google-blue-100)
	}
	.item .display-name {
		margin-left: 8px;
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
<darkengines-select label="User" @search-value-changed="${this.searchValueChanged.bind(this)}" .itemTemplate="${this.itemTemplate.bind(this)}"
 .items="${this.items}" .customStyle="${this.selectStyle}" @items-requested="${this.itemsRequested.bind(this)}" .value="${this.value}">
</darkengines-select>`;
	}
	shouldUpdate(changedProperties) {
		console.log(changedProperties);
		return true;
	}
	itemClick(e, value) {
		e.darkenginesSelect = false;
		this.value = value;
	}
	searchValueChanged(e) {
		fetch('https://localhost:8080', {
			method: 'POST',
			headers: {
				'Accept': 'application/json',
				'Content-Type': 'application/json'
			},
			body: `Users.Where(u => u.DisplayName.StartsWith("${e.detail.value}")).OrderBy(u => u.DisplayName).Skip(0).Take(10)`
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