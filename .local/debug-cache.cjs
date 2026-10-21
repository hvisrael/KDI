const c=require('C:/Program Files/nodejs/node_modules/npm/node_modules/make-fetch-happen/lib/cache/policy');
const orig=c.prototype.satisfies;
c.prototype.satisfies=function(r){const result=orig.call(this,r); if(!result && r.url.includes('playwright')){let h={};for(const n of ['accept','accept-language','accept-encoding','host'])h[n]=[this.request.headers[n],r.headers.get(n)];console.log('CACHE POLICY',JSON.stringify({h,compress:[this.request.compress,r.compress]}))}return result};
