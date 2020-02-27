﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using MessagePack;
using UnityEngine;
using NaughtyAttributes;
using JM.LinqFaster;

[CreateAssetMenu(menuName = "Aetheria/Galaxy")]
public class Galaxy : ScriptableObject
{
	public GalaxyMapData MapData;
	public List<ContinentData> NameDatabase;
}

[Serializable]
public class GalaxyMapData
{
	public int Arms = 4;
	public float Twist = 10;
	public float TwistPower = 2;
	
	[HideInInspector]
	public GalaxyMapLayerData StarDensity;
	[HideInInspector]
	public List<GalaxyMapLayerData> CultureDensities = new List<GalaxyMapLayerData>();
	//[HideInInspector]
	public List<StarData> Stars = new List<StarData>();
}

public static class StarListExtensions
{
	class DijkstraStar
	{
		public float Cost;
		public DijkstraStar Parent;
		public StarData Star;
	}
	
	public static List<StarData> FindPath(this List<StarData> stars, StarData source, StarData target, bool bestFirst = false)
	{
		SortedList<float,DijkstraStar> members = new SortedList<float,DijkstraStar>{{0,new DijkstraStar{Star = source}}};
		List<DijkstraStar> searched = new List<DijkstraStar>();
		while (true)
		{
			var s = members.FirstOrDefault(m => !searched.Contains(m.Value)).Value; // Lowest cost unsearched node
			if (s == null) return null; // No vertices left unsearched
			if (s.Star == target) // We found the path
			{
				Stack<DijkstraStar> path = new Stack<DijkstraStar>(); // Since we start at the end, use a LIFO collection
				path.Push(s);
				while(path.Peek().Parent!=null) // Keep pushing until we reach the start, which has no parent
					path.Push(path.Peek().Parent);
				return path.Select(dv => dv.Star).ToList();
			}
//			foreach (var dijkstraStar in s.Star.Links.Select(i => stars[i]) // All adjacent stars
//				.Where(n => members.All(m => m.Value.Star != n)) // That are not members
//				.Select(n => new DijkstraStar {Parent = s, Star = n, Cost = s.Cost + (s.Star.Position - n.Position).magnitude}))
			// For each adjacent star (filter already visited stars unless heuristic is in use)
			foreach (var dijkstraStar in s.Star.Links.WhereSelectF(i => !bestFirst || members.All(m => m.Value.Star != stars[i]),
				// Cost is parent cost plus distance
				i => new DijkstraStar {Parent = s, Star = stars[i], Cost = s.Cost + (s.Star.Position - stars[i].Position).magnitude}))
				// Add new member to list, sorted by cost plus optional heuristic
				members.Add(bestFirst ? dijkstraStar.Cost + (dijkstraStar.Star.Position - target.Position).magnitude : dijkstraStar.Cost, dijkstraStar);
			searched.Add(s);
		}
	}
}

[Serializable]
public class StarData
{
	public Vector2 Position;
	public List<int> Links = new List<int>();
	public string NameSource;
	public List<string> Names = new List<string>();
}

[Serializable]
public class GalaxyMapLayerData
{
	public float CoreBoost = 1.05f;
	public float CoreBoostOffset = .1f;
	public float CoreBoostPower = 2.25f;
	public float EdgeReduction = 3;
	public float NoiseOffset = 0;
	public float NoiseAmplitude = 1.5f;
	public float NoiseGain = .7f;
	public float NoiseLacunarity = 2;
	public int NoiseOctaves = 7;
	public float NoiseFrequency = 1;
	public int NoiseSeed = 1337;
	
	private FastNoise _noise = new FastNoise();

	public float Evaluate(Vector2 uv, GalaxyMapData galaxy)
	{
		_noise.SetFractalGain(NoiseGain);
		_noise.SetFractalLacunarity(NoiseLacunarity);
		_noise.SetFractalOctaves(NoiseOctaves);
		_noise.SetFrequency(NoiseFrequency);
		_noise.SetSeed(NoiseSeed);
		
		Func<Vector2, Vector2> offset = v => Vector2.one / 2 - v;
		Func<Vector2, float> circle = v => (.5f - v.magnitude) * 2;
		Func<Vector2, Vector2> twist = v => v.Rotate(Mathf.Pow(v.magnitude*2,galaxy.TwistPower)*galaxy.Twist);
		Func<Vector2, float> spokes = v => Mathf.Sin(Mathf.Atan2(-v.y,v.x)*galaxy.Arms);
		var c = circle(offset(uv));
		return Mathf.Clamp01((Mathf.Lerp(spokes(twist(offset(uv))) - EdgeReduction * offset(uv).magnitude, 1,
			    Mathf.Pow(c + CoreBoostOffset, CoreBoostPower) * CoreBoost) - (_noise.GetPerlin(uv.x,uv.y)+NoiseOffset) * NoiseAmplitude) * Mathf.Clamp01(c));
	}
}

public static class Vector2Extension {
     
	public static Vector2 Rotate(this Vector2 v, float degrees) {
		float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
		float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
         
		float tx = v.x;
		float ty = v.y;
		v.x = (cos * tx) - (sin * ty);
		v.y = (sin * tx) + (cos * ty);
		return v;
	}
}

[Serializable]
public class NameData
{
	public Dictionary<string, Dictionary<string, List<string>>> Names = new Dictionary<string, Dictionary<string, List<string>>>();

	public void Add(string dataString)
	{
		//Debug.Log("Adding " + dataString);
		var all = dataString.Split(',');
		var name = all[0].Split(" -.".ToCharArray(),StringSplitOptions.RemoveEmptyEntries).OrderByDescending(s=>s.Length).First();
		var region = all[1].Split('/');
		var continent = region[0];
		var capital = region[1];
		if(!Names.ContainsKey(continent))
			Names[continent] = new Dictionary<string, List<string>>();
		if(!Names[continent].ContainsKey(capital))
			Names[continent][capital] = new List<string>();
		Names[continent][capital].Add(name);
	}
}

[Serializable]
public class ContinentData
{
	public string Name;
	public List<RegionData> Regions;
}

[Serializable]
public class RegionData
{
	public string Name;
	public List<string> Towns;
}

public class VoronoiLink
{
	public Vertex2 point1;
	public Vertex2 point2;

	public float Length { get { return (point1.ToVector2() - point2.ToVector2()).magnitude; } }

	public VoronoiLink(Vertex2 p1, Vertex2 p2)
	{
		point1 = p1;
		point2 = p2;
	}

	public bool ContainsPoint(Vertex2 p)
	{
		return point1 == p || point2 == p;
	}
}

public static class VoronoiLinkExtensions
{
	class DijkstraVertex
	{
		public double Cost;
		public double Heuristic;
		public DijkstraVertex Parent;
		public Vertex2 Vertex;
	}
	
	public static bool ContainsLine(this IEnumerable<VoronoiLink> data, VoronoiLink l)
	{
		return
			data.Any(
				line =>
					(line.point1 == l.point1 && line.point2 == l.point2) ||
					(line.point1 == l.point2 && line.point2 == l.point1));
	}

	public static IEnumerable<Vertex2> FindPath(this IEnumerable<VoronoiLink> data, Vertex2 source, Vertex2 target, bool bestFirst = true)
	{
		List<DijkstraVertex> members = new List<DijkstraVertex>{new DijkstraVertex{Vertex = source}};
		List<Vertex2> searched = new List<Vertex2>();
		while (true)
		{
			var v = members.Where(m=>!searched.Contains(m.Vertex)).OrderBy(m=>bestFirst?m.Heuristic:m.Cost).FirstOrDefault();
			if (v == null) return null; // No vertices left unsearched
			if (v.Vertex == target)
			{
				Stack<DijkstraVertex> path = new Stack<DijkstraVertex>();
				path.Push(v);
				while(path.Peek().Parent!=null)
					path.Push(path.Peek().Parent);
				return path.Select(dv => dv.Vertex).ToList();
			}
			members.AddRange(data.Where(l => l.point1 == v.Vertex).Select(l => l.point2).Concat(data.Where(l => l.point2 == v.Vertex).Select(l => l.point1))
				.Where(n => members.All(m => m.Vertex != n)).Select(n =>
				{
					var c = new DijkstraVertex {Parent = v, Vertex = n, Cost = v.Cost + v.Vertex.Distance(n)};
					if (bestFirst)
						c.Heuristic = c.Cost + c.Vertex.Distance(target);
					return c;
				}));
			searched.Add(v.Vertex);
		}
	}

	public static IEnumerable<Vertex2> ConnectedRegion(this IEnumerable<VoronoiLink> data, Vertex2 v)
	{
		List<Vertex2> members = new List<Vertex2>();
		members.Add(v);
		while (true)
		{
			int lastCount = members.Count;
			// For each member, add all vertices that are connected to it via a line but are not already a member
			foreach (Vertex2 m in members.ToArray())
				members.AddRange(data.Where(l => l.point1 == m).Select(l => l.point2).Concat(data.Where(l => l.point2 == m).Select(l => l.point1))
					.Where(n => !members.Contains(n)));
			// If we have stopped finding neighbors, stop traversing
			if (members.Count == lastCount)
				return members;
		}
	}
}