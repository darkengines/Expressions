import Expression, { LambdaExpression } from './Expression';

export interface IEnumerable<TSource> {

}

export interface IEqualityComparer<TSource> {

}

export default class Queryable<TSource> {
	public Any(predicate: LambdaExpression<(source: TSource) => boolean>): Queryable<TSource> {
		return this;
	}
}