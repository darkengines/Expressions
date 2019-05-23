//import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import { templatize } from '@polymer/polymer/lib/utils/templatize';
import { html, LitElement } from '@polymer/lit-element/lit-element';
import '@polymer/paper-input/paper-input.js';
import '@polymer/paper-icon-button/paper-icon-button.js';
import '@polymer/iron-icons/iron-icons.js';
import '@polymer/paper-ripple/paper-ripple.js';
import '@polymer/iron-icon/iron-icon.js';
import { classes } from '../dom-helper';
import * as bodyScrollLock from 'body-scroll-lock/lib/bodyScrollLock.es6.js';

class DarkenginesSelect extends LitElement {
	getOffset(element, offset) {
		if (offset === undefined) offset = {x: 0, y: 0};
		if (element) {
			offset.x += element.offsetLeft;
			offset.y += element.offsetTop;
			return this.getOffset(element.offsetParent, offset);
		}
		return offset;
	}
	windowResize(e) {
		this.mobile = !window.matchMedia("(min-width: 800px)").matches;
	}
	constructor() {
		super();
		this.searchText = '';
		window.addEventListener('resize', (e) => this.windowResize(e));
		this.windowResize();
		window.addEventListener('click', e => {
			var path = e.composedPath();
			if (path[0] !== this) {
				this.focused = path.some(element => element === this.shadowRoot);
				var opened =this.focused && !this.willClose;
				if (opened && !this.opened) {
					var bodyScroll = window.scrollY;
					this.previousScroll = bodyScroll;
					var searchOffset = this.getOffset(this.$search.parentNode);
					//document.body.scrollTop = searchOffset.y;
					bodyScrollLock.disableBodyScroll(this.$items);
					if (!this.$inputSearch.focused) {
						this.$inputSearch.focus();
					}
					this.$search.style.transition = 'none';
					this.$search.style.visibility = 'visible';
					this.$search.style.top = `${searchOffset.y - bodyScroll + this.$search.parentNode.clientHeight}px`;
					this.$search.style.left = `${searchOffset.x}px`;
					this.$search.style.right = `${searchOffset.x + this.$search.parentNode.clientWidth}px`;
					this.$search.style.bottom = `${searchOffset.y - bodyScroll + this.$search.parentNode.clientHeight}px`;
					this.$search.style.minWidth = `${this.$search.parentNode.clientWidth}px`;
					this.$search.style.transition = 'top 0.2s, bottom 0.2s, left 0.2s, right 0.2s, height 0.2s, width 0.2s, visibility 0.2s';
					this.opened = opened;
				} else if (!opened && this.opened) {
					var bodyScroll = window.scrollY;
					this.previousScroll = bodyScroll;
					var searchOffset = this.getOffset(this.$search.parentNode);
					this.$search.style.top = `${searchOffset.y - bodyScroll + this.$search.parentNode.clientHeight}px`;
					this.$search.style.left = `${searchOffset.x}px`;
					this.$search.style.right = `${searchOffset.x + this.$search.parentNode.clientWidth}px`;
					this.$search.style.bottom = `${searchOffset.y - bodyScroll + this.$search.parentNode.clientHeight}px`;
					this.$search.style.visibility = 'hidden';
					bodyScrollLock.enableBodyScroll(this.$items);
					
					if (this.$inputSearch.focused) {
						this.$inputSearch.blur();
					}
					this.opened = opened;
				}
				this.willClose = false;
			}
		});
		this.canRequestItems = true;
		this.itemIndex = 0;
	}
	itemClick(item) {
		this.value = item;
		//this.opened = false;
		this.willClose = true;
	}
	getItemTemplate(item, index, itemIndex) {
		return html`
		<div class="${classes({
						item: true,
						selected: item === this.value,
						hover: index === itemIndex
					})}"
			@click="${(e) => this.itemClick(item)}">
			${this.itemTemplate ? this.itemTemplate(item, index, itemIndex) : item}
		</div>`;
	}
	getSelectedItemTemplate(item) {
		return html`
			<div class="selected-item">
				${item ?
					(this.selectedItemTemplate ? this.selectedItemTemplate(item) : item)
					: this.emptySelectedItemTemplate ? this.emptySelectedItemTemplate() : 'Select'
				}
			</div>`;
	}
	firstUpdated() {
		this.$items = this.shadowRoot.querySelector('#items');
		this.$inputSearch = this.shadowRoot.querySelector('#inputSearch');
		this.$dropDownButton = this.shadowRoot.querySelector('#drop-down-button');
		this.$input = this.shadowRoot.querySelector('#input');
		this.$search = this.shadowRoot.querySelector('#search');
		this.$items = this.shadowRoot.querySelector('#items');
		this.$search.addEventListener('transitionend', e => {
			this.itemScrollChanged(null);
			if(this.opened && !this.$inputSearch.focused) this.$inputSearch.focus();
		});
	}

	itemScrollChanged(e) {
		var progress = (Math.min(this.$items.clientHeight, window.innerHeight) + this.$items.scrollTop) / this.$items.scrollHeight;
		if (progress > 0.9 && this.canRequestItems) {
			this.canRequestItems = false;
			this.itemsRequested({
				searchText: this.searchText,
				index: this.items.length,
				count: 10,
			}).then((items) => {
				this.items = [...this.items, ...items];
				return this.updateComplete.then(e => items);
			}).then(e => {
				if (e.length) this.itemScrollChanged();
			});
		}
	}
	searchValueChanged(e) {
		this.searchText = e.detail.value;
		this.itemIndex = 0;
		if (this.$items) {
			this.$items.scrollTop = 0;
		}
		this.itemsRequested({
			searchText: this.searchText,
			index: 0,
			count: 1,
		}).then((items) => {
			this.items = items;
			return this.updateComplete;
		}).then(e => {
			this.itemScrollChanged();
		});
	}
	updated(changedProperties) {
		if (changedProperties.has('items')) {
			this.canRequestItems = true;
		}
		if (changedProperties.has('opened')) {
			if (changedProperties['opened']) {
				this.$inputSearch.focus();
			}
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
			opened: Boolean,
			value: Object,
			itemIndex: Number,
			mobile: Boolean,
			scrollTop: Number
		}
	}
	renderItems(items) {
		return items.length ?
			items.map((item, index) => this.getItemTemplate(item, index, this.itemIndex))
			: 'No result';
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
			this.opened = false;
			this.$inputSearch.blur();
		}
	}
	inputClick(e) {
		this.willClose = this.opened;
	}
	render() {
		var dropDownIcon = html`<iron-icon id="drop-down-button" class="${classes({ 'drop-down-icon': true, opened: this.opened })}" icon="arrow-drop-down"></iron-icon>`;
		return html`<style>
	* {
		box-sizing: border-box;
	}
	:host {
		display: block;
	}

	.drop-down-icon {
		transition: transform 0.2s;
	}

	.drop-down-icon.opened {
		transform: rotateZ(180deg);
		transition: transform 0.2s;
	}
	.search {
		position: fixed;
		visibility: hidden;
		overflow: hidden;
		background-color: white;
		transition: top 0.2s, bottom 0.2s, left 0.2s, right 0.2s, height 0.2s, width 0.2s, visibility 0.2s;
		z-index: 2;
		display: flex;
		flex-direction: column;
		-webkit-overflow-scrolling: touch;
	}

	.focused .search {
		overflow: inherit;
	}

	.mobile.focused .search {
		height: auto;
		top: 0 !important;
		bottom: 0 !important;
		left: 0 !important;
		right: 0 !important;
	}
	.mobile.focused .items {
		overflow: auto;
	}
	.mobile .items {
		border-bottom: none;
		border-left: none;
		border-right: none;
	}
	.items {
		-webkit-overflow-scrolling: touch;
		overflow-y: hidden;
		height: 100%;
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
	.items .item.hover.selected,
	.items .item:nth-child(2n).selected:hover,
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
		cursor: pointer;
		position: relative;
	}
	.input .item {
		width: 100%;
	}
	.select {
		position: relative;
	}
	.items .item.hover {
		background-color: var(--google-blue-100)
	}
	.mobile .items .item.hover {
		background-color: inherit;
	}
	.mobile #inputSearch {
		margin: 0 8px;
	}
	.big-label {
			--big-height: 22px;
          --paper-input-container-label: {
            font-size: var(--big-height);
            line-height: calc(var(--big-height) + 4px);
          };
          --paper-input-container-input: {
            font-size: var(--big-height);
          };
          --paper-input-container: {
            line-height: calc(var(--big-height) + 4px);
          };
        }
</style>
${this.customStyle()}
<div class="${classes({select: true, focused: this.opened, mobile: this.mobile})}" @click="${this.click.bind(this)}">
	<div id="input" class="input" @click="${this.inputClick.bind(this)}">
		${this.getSelectedItemTemplate(this.value)} ${dropDownIcon}
	</div>
	<div id="search" class="search">
		<paper-input @keydown="${this.keyDown.bind(this)}" no-label-float id="inputSearch" label="Search" @value-changed="${this.searchValueChanged.bind(this)}"
		 .value="${this.searchText}" @focused-changed="${e => console.log(e)}" class="big-label">
		</paper-input>
		<div id="items" class="items" @scroll="${this.itemScrollChanged.bind(this)}">
			${this.renderItems(this.items)}
		</div>
	</div>
</div>`;
	}
}

window.customElements.define('darkengines-select', DarkenginesSelect);