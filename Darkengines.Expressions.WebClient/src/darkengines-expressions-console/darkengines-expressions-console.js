
import { PolymerElement, html } from '@polymer/polymer/polymer-element.js';
import { resolve, encode, decode } from '../json-ref';

class DarkenginesExpressionsConsole extends PolymerElement {
	static get template() {
		return html`
	<style>
		:host {
			display: flex;
		}
	
		* {
			box-sizing: border-box;
		}
	
		.console {
			background-color: rgba(33, 33, 33, 1.0);
			height: 100%;
			width: 100%;
			display: flex;
			flex-direction: column;
			padding: 16px;
			color: rgba(198, 198, 198, 1.0);
			font-size: 8px;
		}
	
		.console .output {
			display: flex;
			flex-direction: row;
			height: 100%;
			font-family: monospace;
		}
	
		.console .output .result {
			overflow-y: auto;
			width: 50%;
			padding: 4px;
			border-left: 1px solid rgba(198, 198, 198, 1.0);
			border-right: 1px solid rgba(198, 198, 198, 1.0);
			border-top: 1px solid rgba(198, 198, 198, 1.0);
		}
	
		.console .output .logs {
			overflow-y: auto;
			padding: 4px;
			width: 50%;
			border-right: 1px solid rgba(198, 198, 198, 1.0);
			border-top: 1px solid rgba(198, 198, 198, 1.0);
		}
	
		.console .input {
			width: 100%;
			height: 64px;
			border: 1px solid rgba(198, 198, 198, 1.0);
			background-color: inherit;
			color: inherit;
			outline: 0;
			padding: 4px;
			font-size: 8px;
			resize: none;
		}
	</style>
	<div class="console">
		<div class="output">
			<div id="result" class="result">
			</div>
			<div class="logs">
			</div>
		</div>
		<textarea id="input" on-keydown="inputKeyDown" class="input" spellcheck="false"></textarea>
	</div>
    `;
	}
	inputKeyDown(e) {
		if (e.keyCode === 13) {
			fetch('https://localhost:8080', {
				method: 'POST',
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json'
				},
				body: this.$.input.value
			}).then(response => {
				return response.json().then(json => {
					this.$.result.innerHTML = JSON.stringify(json);
					var decoded = resolve(json);
					console.log(decoded);
				});
			});
			e.preventDefault();
			return false;
		}
	}
	static get properties() {
		return {

		};
	}
}

window.customElements.define('darkengines-expressions-console', DarkenginesExpressionsConsole);