














//AUTOGENERATED, DO NOTMODIFY.
//Do not edit this file directly.

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
// ReSharper disable CheckNamespace

using System;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;
using System.Collections;
using System.Collections.Generic;


namespace RethinkDb.Driver.Ast {

    public partial class Update : ReqlExpr {

    
    
    
/// <summary>
/// <para>Update JSON documents in a table. Accepts a JSON document, a ReQL expression, or a combination of the two.</para>
/// </summary>
/// <example><para>Example: Update the status of the post with <code>id</code> of <code>1</code> to <code>published</code>.</para>
/// <code>r.table("posts").get(1).update({status: "published"}).run(conn, callback)
/// </code></example>
        public Update (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Update JSON documents in a table. Accepts a JSON document, a ReQL expression, or a combination of the two.</para>
/// </summary>
/// <example><para>Example: Update the status of the post with <code>id</code> of <code>1</code> to <code>published</code>.</para>
/// <code>r.table("posts").get(1).update({status: "published"}).run(conn, callback)
/// </code></example>
        public Update (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Update JSON documents in a table. Accepts a JSON document, a ReQL expression, or a combination of the two.</para>
/// </summary>
/// <example><para>Example: Update the status of the post with <code>id</code> of <code>1</code> to <code>published</code>.</para>
/// <code>r.table("posts").get(1).update({status: "published"}).run(conn, callback)
/// </code></example>
        public Update (Arguments args, OptArgs optargs)
         : base(TermType.UPDATE, args, optargs) {
        }


    



    
///<summary>
/// "durability": "E_DURABILITY",
///  "return_changes": [
///    "T_BOOL",
///    "always"
///  ],
///  "non_atomic": "T_BOOL"
///</summary>
        public Update this[object optArgs] {
            get
            {
                var newOptArgs = OptArgs.FromMap(this.OptArgs).With(optArgs);
        
                return new Update (this.Args, newOptArgs);
            }
        }
        
///<summary>
/// "durability": "E_DURABILITY",
///  "return_changes": [
///    "T_BOOL",
///    "always"
///  ],
///  "non_atomic": "T_BOOL"
///</summary>
    public Update this[OptArgs optArgs] {
        get
        {
            var newOptArgs = OptArgs.FromMap(this.OptArgs).With(optArgs);
    
            return new Update (this.Args, newOptArgs);
        }
    }
    
///<summary>
/// "durability": "E_DURABILITY",
///  "return_changes": [
///    "T_BOOL",
///    "always"
///  ],
///  "non_atomic": "T_BOOL"
///</summary>
        public Update OptArg(string key, object val){
            
            var newOptArgs = OptArgs.FromMap(this.OptArgs).With(key, val);
        
            return new Update (this.Args, newOptArgs);
        }
        internal Update optArg(string key, object val){
        
            return this.OptArg(key, val);
        }


    

    
        /// <summary>
        /// Get a single field from an object. If called on a sequence, gets that field from every object in the sequence, skipping objects that lack it.
        /// </summary>
        /// <param name="bracket"></param>
        public new Bracket this[string bracket] => base[bracket];
        
        /// <summary>
        /// Get the nth element of a sequence, counting from zero. If the argument is negative, count from the last element.
        /// </summary>
        /// <param name="bracket"></param>
        /// <returns></returns>
        public new Bracket this[int bracket] => base[bracket];


    

    


    
    }
}