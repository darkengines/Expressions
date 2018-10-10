export type LambdaExpression<T> = Expression<T> | T;

export default class Expression<T> {
	public constructor(expression: T) { this.expression = expression.toString(); }
	public expression: string;
}