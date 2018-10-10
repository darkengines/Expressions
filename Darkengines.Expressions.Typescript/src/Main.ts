import Queryable from './Queryable';

var query = new Queryable<number>().Any(x => x % 2 == 0);