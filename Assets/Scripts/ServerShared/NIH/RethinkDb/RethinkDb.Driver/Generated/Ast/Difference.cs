














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

    public partial class Difference : ReqlExpr {

    
    
    
/// <summary>
/// <para>Remove the elements of one array from another array.</para>
/// </summary>
/// <example><para>Example: Retrieve Iron Man's equipment list without boots.</para>
/// <code>r.table('marvel').get('IronMan')('equipment')
///   .difference(['Boots'])
///   .run(conn, callback)
/// </code></example>
        public Difference (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Remove the elements of one array from another array.</para>
/// </summary>
/// <example><para>Example: Retrieve Iron Man's equipment list without boots.</para>
/// <code>r.table('marvel').get('IronMan')('equipment')
///   .difference(['Boots'])
///   .run(conn, callback)
/// </code></example>
        public Difference (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Remove the elements of one array from another array.</para>
/// </summary>
/// <example><para>Example: Retrieve Iron Man's equipment list without boots.</para>
/// <code>r.table('marvel').get('IronMan')('equipment')
///   .difference(['Boots'])
///   .run(conn, callback)
/// </code></example>
        public Difference (Arguments args, OptArgs optargs)
         : base(TermType.DIFFERENCE, args, optargs) {
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