//import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import { templatize } from '@polymer/polymer/lib/utils/templatize';
import { html, LitElement } from '@polymer/lit-element/lit-element';
import '@polymer/paper-input/paper-input.js';
import '@polymer/paper-icon-button/paper-icon-button.js';
import '@polymer/iron-icons/iron-icons.js';

class DarkenginesSelect extends LitElement {
	constructor() {
		super();
		this.searchText = '';
		window.addEventListener('click', e => {
			this.focused = !!e.darkenginesSelect;
		});
		this.canRequestItems = true;
		this.itemIndex = 0;
	}
	firstUpdated() {
		this.$items = this.shadowRoot.querySelector('#items');
	}
	itemScrollChanged(e) {
		var progress = (this.$items.clientHeight + this.$items.scrollTop) / this.$items.scrollHeight;
		if (progress > 0.9 && this.canRequestItems) {
			this.canRequestItems = false;
			this.dispatchEvent(new CustomEvent('items-requested', {
				detail: {
					searchText: this.searchText,
					index: this.items.length,
					count: 10,
				}
			}));
		}
	}
	click(e) {
		e.darkenginesSelect = (e.darkenginesSelect === undefined || e.darkenginesSelect) && true;
		//this.shadowRoot.querySelector('#inputSearch').focus();
	}
	defaultItemTemplate(item, index) {
		return html`<div class="item ${index == this.itemIndex} ? 'hover' : ''">${item}</div>`;
	}
	searchValueChanged(e) {
		this.searchText = e.detail.value;
		this.itemIndex = 0;
		if (this.$items) {
			this.$items.scrollTop = 0;
		}
		this.dispatchEvent(new CustomEvent('search-value-changed', { detail: { value: e.detail.value } }));
	}
	updated(changedProperties) {
		if (changedProperties.has('items')) {
			this.canRequestItems = true;
		}
	}
	static get properties() {
		return {
			searchText: String,
			items: {
				type: Array,
				value: []
			},
			label: String,
			focused: Boolean,
			inputFocused: Boolean,

			value: Object,
			itemIndex: Number
		}
	}
	focusedChanged(e) {
		//this.focused = e.detail.value;
	}
	onFocus(e) {
		if (!this.focused) this.focused = true;
	}
	onBlur(e) {
		this.focused = false;
	}
	renderItems(items) {
		return items.length ?
			items.map(((item, index) => this.itemTemplate(item, index, this.itemIndex)) || this.defaultItemTemplate)
			: this.defaultItemTemplate('No result');
	}
	keyDown(e) {
		if (e.keyCode === 40 && this.itemIndex < this.items.length - 1) {
			this.itemIndex++;
			this.$items.children[this.itemIndex].scrollIntoView();
		}
		if (e.keyCode === 38 && this.itemIndex > 0) {
			this.itemIndex--;
			this.$items.children[this.itemIndex].scrollIntoView();
		}
		if (e.keyCode === 13) {
			this.value = this.items[this.itemIndex];
		}
	}
	render() {
		return html`<style>
	:host {
		display: block;
	}

	.search {
		overflow-y: auto;
		max-height: 0;
		display: flex;
		flex-direction: column;
	}

	.focused .search {
		max-height: 256px;
		transition: max-height 0.2s;
		overflow-y: hidden;
	}

	.items {
		overflow-y: auto;
		margin-top: -8px;
		border-bottom: solid 1px var(--paper-input-container-color, var(--secondary-text-color));
		border-left: solid 1px var(--paper-input-container-color, var(--secondary-text-color));
		border-right: solid 1px var(--paper-input-container-color, var(--secondary-text-color));
	}

	.items .item:hover,
	.items .item:nth-child(2n):hover {
		background-color: var(--google-blue-100);
	}

	.item {
		cursor: pointer;
	}

	.items .item.selected,
	.items .item:nth-child(2n).selected {
		background-color: var(--primary-color);
		color: var(--dark-theme-text-color);
	}

	.items .item:nth-child(2n) {
		background-color: var(--google-grey-100);
	}

	.input {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
</style>
${this.customStyle()}
<div tabindex="0" @focusin="${this.onFocus.bind(this)}" @focusout="${this.onBlur.bind(this)}" class="select ${this.focused ? 'focused' : ''}"
 @click="${this.click.bind(this)}">
	<div class="input">
		${(this.value && this.itemTemplate(this.value)) || 'Select'}
		<paper-icon-button icon="arrow-drop-down"></paper-icon-button>
	</div>
	<div class="search" tabindex="0">
		<paper-input @keydown="${this.keyDown.bind(this)}" no-label-float id="inputSearch" label="Search" @value-changed="${this.searchValueChanged.bind(this)}"
		 .value="${this.searchText}">
		</paper-input>
		<div id="items" class="items" @scroll="${this.itemScrollChanged.bind(this)}">
			${this.renderItems(this.items)}
		</div>
	</div>
</div>`;
	}
}

window.customElements.define('darkengines-select', DarkenginesSelect);