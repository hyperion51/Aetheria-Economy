﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MessagePack;
using UniRx;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public class ZoneGenerator
{
	public static ZonePack GenerateZone(
		ZoneGenerationSettings settings,
		Dictionary<string,GalaxyMapLayerData> mapLayers,
		IEnumerable<SimpleCommodityData> resources,
		string name,
		float2 position)
	{
		var random = new Random(Convert.ToUInt32(abs(name.GetHashCode())));
		var radius = settings.ZoneRadius.Evaluate(random.NextFloat() * mapLayers["Radius"].Evaluate(position, settings.ShapeSettings));
		var mass = settings.ZoneMass.Evaluate(random.NextFloat() * mapLayers["Mass"].Evaluate(position, settings.ShapeSettings));

		return GenerateZone(settings, mapLayers, resources, name, position, mass, radius, random);
	}

	public static ZonePack GenerateZone(
		ZoneGenerationSettings settings,
		Dictionary<string,GalaxyMapLayerData> mapLayers,
		IEnumerable<SimpleCommodityData> resources,
		float mass,
		float radius)
	{
		var name = Guid.NewGuid().ToString().Substring(0, 8);
		var position = float2(0f);
		var random = new Random(Convert.ToUInt32(abs(name.GetHashCode())));
		
		return GenerateZone(settings, mapLayers, resources, name, position, mass, radius, random);
	}
	
	private static ZonePack GenerateZone(
		ZoneGenerationSettings settings,
		Dictionary<string,GalaxyMapLayerData> mapLayers,
		IEnumerable<SimpleCommodityData> resources,
		string name,
		float2 position,
		float mass,
		float radius,
		Random random)
	{
		var zone = new ZoneData
		{
			Name = name, 
			Position = position
		};
		
		var pack = new ZonePack
		{
			Data = zone
		};
		
		zone.Radius = radius;
		zone.Mass = mass;
		
		//Debug.Log($"Generating zone at position {zone.Position} with radius {zoneRadius} and mass {zoneMass}");
		
		var planets = GenerateEntities(settings, ref random, zone);
        
        // Create collections to map between zone generator output and database entries
        var orbitMap = new Dictionary<GeneratorPlanet, OrbitData>();
        var orbitInverseMap = new Dictionary<OrbitData, GeneratorPlanet>();
        
        // Create orbit database entries
        pack.Orbits = planets.Select(planet =>
        {
            var data = new OrbitData
            {
                Distance = new ReactiveProperty<float>(planet.Distance),
                //Period = planet.Period,
                Phase = planet.Phase
            };
            orbitMap[planet] = data;
            orbitInverseMap[data] = planet;
            return data;
        }).ToList();

        // Link OrbitData parents to database GUIDs
        foreach (var data in pack.Orbits)
            data.Parent = orbitInverseMap[data].Parent != null
                ? orbitMap[orbitInverseMap[data].Parent].ID
                : Guid.Empty;
        
        // Cache resource densities
        var resourceMaps = mapLayers.Values
	        .ToDictionary(m => m.ID, m => m.Evaluate(zone.Position, settings.ShapeSettings));
        
        pack.Planets = planets.Where(p=>!p.Empty).Select(planet =>
        {
	        Dictionary<Guid, float> planetResources = new Dictionary<Guid, float>();
	        BodyType bodyType = planet.Belt ? BodyType.Asteroid :
		        planet.Mass > settings.SunMass ? BodyType.Sun :
		        planet.Mass > settings.GasGiantMass ? BodyType.GasGiant :
		        planet.Mass > settings.PlanetMass ? BodyType.Planet : BodyType.Planetoid;
	        
	        foreach (var r in resources)
	        {
		        if ((bodyType & r.ResourceBodyType) != 0)
		        {
			        float quantity = ResourceValue(ref random, settings, r, r.ResourceDensity.Aggregate(1f, (m, rdm) => m * resourceMaps[rdm]));
			        if (r.Floor < quantity) planetResources.Add(r.ID, quantity);
		        }
	        }

	        BodyData planetData;
	        switch (bodyType)
	        {
		        case BodyType.Asteroid:
			        planetData = new AsteroidBeltData();
			        break;
		        case BodyType.Planetoid:
		        case BodyType.Planet:
			        planetData = new PlanetData();
			        break;
		        case BodyType.GasGiant:
			        planetData = new GasGiantData();
			        break;
		        case BodyType.Sun:
			        planetData = new SunData();
			        break;
		        default:
			        throw new ArgumentOutOfRangeException();
	        }

	        planetData.Mass.Value = planet.Mass;
	        planetData.Orbit = orbitMap[planet].ID;
	        planetData.Resources = planetResources;
            planetData.Name.Value = planetData.ID.ToString().Substring(0, 8);
            if (planetData is AsteroidBeltData beltData)
            {
	            beltData.Asteroids = 
		            Enumerable.Range(0, (int) (settings.AsteroidCount.Evaluate(beltData.Mass.Value * orbitMap[planet].Distance.Value)))
			            .Select(_ => new Asteroid
			            {
				            Distance = random.NextFloat(orbitMap[planet].Distance.Value * .5f, orbitMap[planet].Distance.Value * 1.5f),
				            Phase = random.NextFloat(),
				            Size = random.NextFloat(),
				            RotationSpeed = settings.AsteroidRotationSpeed.Evaluate(random.NextFloat())
			            })
			            //.OrderByDescending(a=>a.Size)
			            .ToArray();
            }
            else if (planetData is GasGiantData gas)
            {
	            var times = Enumerable.Range(0, 5).Select(i => (float) i / 4).ToArray();
	            gas.Colors.Value = times.Select(time => float4(random.NextFloat3(),time)).ToArray();
	            gas.AlbedoRotationSpeed.Value = -3;
	            gas.FirstOffsetRotationSpeed.Value = 5;
	            gas.FirstOffsetDomainRotationSpeed.Value = 0;
	            gas.SecondOffsetRotationSpeed.Value = 10;
	            gas.SecondOffsetDomainRotationSpeed.Value = -25;
	            if (planetData is SunData sun)
	            {
		            sun.FogTintColor.Value = sun.LightColor.Value = random.NextFloat3();
		            gas.FirstOffsetDomainRotationSpeed.Value = 5;
	            }
            }
            return planetData;
        }).ToList();

        return pack;
	}
	
	static float ResourceValue(ref Random random, ZoneGenerationSettings settings, SimpleCommodityData resource, float density)
	{
		return random.NextPowerDistribution(resource.Minimum, resource.Maximum, resource.Exponent,
			1 / lerp(settings.ResourceDensityMinimum, settings.ResourceDensityMaximum, density));
	}

	public static GeneratorPlanet[] GenerateEntities(ZoneGenerationSettings settings, ref Random random, ZoneData zone)
	{
		var root = new GeneratorPlanet
		{
			Settings = settings,
			Mass = zone.Mass,
			ChildDistanceMaximum = zone.Radius * .75f,
			ChildDistanceMinimum = settings.PlanetSafetyRadius.Evaluate(zone.Mass)
		};

		// There is some chance of generating a rosette or binary system
		// Probabilities which are fixed for the entire galaxy are in GlobalData, contained in the GameContext
		var rosette = random.NextFloat() < settings.RosetteProbability;
		
		if (rosette)
		{
			// Create a rosette with a number of vertices between 2 and 9 inclusive
			root.ExpandRosette(ref random, (int)(random.NextFloat(1, 5) + random.NextFloat(1, 5)));
		
			// Create a small number of less massive "captured" planets orbiting past the rosette
			root.ExpandSolar(
				ref random,
				count: (int)(random.NextFloat(1, 3) * random.NextFloat(1, 2)), 
				massMulMin: .6f, 
				massMulMax: .8f, 
				distMulMin: 1.25f, 
				distMulMax: 1.75f, 
				jupiterJump: 1,
				massFraction: .1f);

			var averageChildMass = root.Children.Sum(p => p.Mass) / root.Children.Count;
			foreach(var p in root.Children.Where(c=>c.Mass>settings.GasGiantMass))
			{
				var m = p.Mass / averageChildMass;
				// Give each child in the rosette its own mini solar system
				p.ExpandSolar(
					ref random,
					count: (int) (random.NextFloat(1, 3 * m) + random.NextFloat(1, 3 * m)), 
					massMulMin: 0.75f, 
					massMulMax: 2.5f, 
					distMulMin: 1 + m * .25f,
					distMulMax: 1.05f + m * .5f,
					jupiterJump: random.NextFloat() * random.NextFloat() * 10 + 1,
					massFraction: .5f
				);
			}
		}
		else
		{
			// Create a regular old boring solar system
			root.ExpandSolar(
				ref random,
				count: random.NextInt(5, 15), 
				massMulMin: 0.5f, 
				massMulMax: 2.0f, 
				distMulMin: 1.1f,
				distMulMax: 1.25f,
				jupiterJump: random.NextFloat() * random.NextFloat() * 10 + 1,
				massFraction: .25f
			);
		}

		var alreadyExpanded = new List<GeneratorPlanet>();
		var binaries = new List<GeneratorPlanet>();
		for(int i=0; i<settings.SatellitePasses; i++)
		{
			// Get all children that are above the satellite creation mass floor and not rosette members
			var satelliteCandidates = rosette
				? root.AllPlanets().Where(p => 
					p != root && 
					p.Parent != root && 
					p.Mass > settings.SatelliteCreationMassFloor &&
					!alreadyExpanded.Contains(p))
				: root.AllPlanets().Where(p => 
					p != root && 
					p.Mass > settings.SatelliteCreationMassFloor &&
					!alreadyExpanded.Contains(p));

			foreach (var planet in satelliteCandidates)
			{
				// There's a chance of generating satellites for each qualified planet
				if (random.NextFloat() < settings.SatelliteCreationProbability)
				{
					// Sometimes the satellite is so massive that it forms a binary system (like Earth!)
					if (random.NextFloat() < settings.BinaryCreationProbability)
					{
						planet.ExpandRosette(ref random, 2);
						binaries.AddRange(planet.Children);
					}
					// Otherwise, terrestrial planets get a couple satellites while gas giants get many
					else
					{
						planet.ExpandSolar(
							ref random,
							count: planet.Mass < settings.GasGiantMass ? random.NextInt(1, 3) : random.NextInt(2, 6),
							massMulMin: .75f,
							massMulMax: 1.5f,
							distMulMin: 1.05f,
							distMulMax: 1.25f,
							jupiterJump: 1,
							massFraction: .15f); // Planetary satellites are not nearly as massive as planets themselves
					}
					alreadyExpanded.Add(planet);
				}
			}
		}

		// Get all children that are below the belt creation mass floor and not rosette members, also exclude binaries
		var beltCandidates = rosette
			? root.AllPlanets().Where(p => p != root && p.Parent != root && p.Mass < settings.BeltMassCeiling && !binaries.Contains(p))
			: root.AllPlanets().Where(p => p != root && p.Mass < settings.BeltMassCeiling && !binaries.Contains(p));
		
		foreach(var planet in beltCandidates.Reverse())
			if (random.NextFloat() < settings.BeltProbability && !planet.Parent.Children.Any(p=>p.Belt))
				planet.Belt = true;

		var totalMass = root.AllPlanets().Sum(p => p.Mass);
		var rootMass = root.Mass;
		foreach (var planet in root.AllPlanets())
			planet.Mass = (planet.Mass / totalMass) * rootMass;

		return root.AllPlanets().ToArray();
	}
}

// All orbiting bodies are referred to here as "Planets"
// This goes back to the original etymology for the word planet, which is traveler,
// Because they were objects we observed to move in the sky instead of remaining fixed like the stars
public class GeneratorPlanet
{
	public ZoneGenerationSettings Settings;
	public float Distance;
	public float Phase;
	public float Mass;
	//public float Period;
	public float ChildDistanceMinimum;
	public float ChildDistanceMaximum;
	public bool Empty = false;
	public bool Belt = false;
	public List<GeneratorPlanet> Children = new List<GeneratorPlanet>(); // Planets orbiting this one are referred to as children
	public GeneratorPlanet Parent;

	// Recursively gather all planets in the hierarchy
	public IEnumerable<GeneratorPlanet> AllPlanets()
	{
		return new[]{this}.Concat(Children.SelectMany(c=>c.AllPlanets()));
	}

	// Create children that fill a single orbit, equally spaced
	// https://en.wikipedia.org/wiki/Klemperer_rosette
	public void ExpandRosette(ref Random random, int vertices)
	{
		//Debug.Log("Expanding Rosette");
		
		// Rosette children replace the parent, parent orbital node is left empty
		Empty = true;
		
		// Masses in a rosette alternate, so every sequential pair has the same shared mass
		var sharedMass = Mass / vertices * 2;
		
		// Proportion of the masses of sequential pairs is random, but only for even vertex counts
		var proportion = vertices % 2 == 0 ? random.NextFloat(.5f,.95f) : .5f;
		
		// Place children at a fixed distance in the center of the range
		var dist = (ChildDistanceMinimum + ChildDistanceMaximum) / 2;
		
		// Position of first child
		var p0 = new float2(0, dist);
		
		// Position of second child
		var p1 = OrbitData.Evaluate(1.0f / vertices) * dist;
		
		// Maximum child distance is half the distance to the neighbor minus the neighbor's radius
		var p0ChildDist = (distance(p0, p1) * proportion - Settings.PlanetSafetyRadius.Evaluate(sharedMass * (1 - proportion))) * .75f;
		var p1ChildDist = (distance(p0, p1) * (1 - proportion) - Settings.PlanetSafetyRadius.Evaluate(sharedMass * proportion)) * .75f;
		
		for (int i = 0; i < vertices; i++)
		{
			var child = new GeneratorPlanet
			{
				Settings = Settings,
				Parent = this,
				Mass = sharedMass * (i % 2 == 0 ? proportion : 1 - proportion), // Masses alternate
				Distance = dist,
				Phase = (float) i / vertices,
				ChildDistanceMaximum = (i % 2 == 0 ? p0ChildDist : p1ChildDist)
			};
			child.ChildDistanceMinimum = Settings.PlanetSafetyRadius.Evaluate(child.Mass) * 2;
			//child.Period = Settings..Evaluate(child.Distance);
			Children.Add(child);
		}

		ChildDistanceMinimum = dist + p0ChildDist;
	}

	// Create children that mimic the distribution of planetary masses in the solar system
	public void ExpandSolar(ref Random random, int count, float massMulMin, float massMulMax, float distMulMin, float distMulMax, float jupiterJump, float massFraction)
	{
		//Debug.Log("Expanding Solar");
		// Expansion is impossible when child space is full
		if (count == 0 || ChildDistanceMaximum < ChildDistanceMinimum)
			return;
		
		var masses = new float[count];
		var distances = new float[count];
		
		// Accumulate total mass 
		// Initialize first mass and distance, actual number doesn't matter since total mass will be divided proportionally
		float massTotal = distances[0] = masses[0] = 1;
		
		// Masses and distances multiply from one planet to the next
		// Mass typically increases exponentially as you go further out
		// jupiterJump is an additional mass multiplier applied after half of the planets
		for (var i = 1; i < count; i++)
		{
			massTotal += masses[i] = masses[i - 1] * random.NextFloat(massMulMin, massMulMax) * (count/2==i ? jupiterJump : 1);
			distances[i] = distances[i - 1] * random.NextFloat(distMulMin, distMulMax);
		}

		// Add some randomness to child masses
		for (var i = 0; i < masses.Length; i++)
			masses[i] *= random.NextFloat(.1f, 1f);

		// Normalize the masses of the children
		for (var i = 0; i < count; i++)
			masses[i] = masses[i] / massTotal * Mass * massFraction;

		// Map child distances to range between minimum and maximum
		if (count > 1)
		{
			var oldDistances = (float[]) distances.Clone();
			for (var i = 0; i < count; i++)
				distances[i] = lerp(ChildDistanceMinimum, ChildDistanceMaximum,
					(oldDistances[i] - oldDistances[0]) / (oldDistances[count - 1] - oldDistances[0])) + Settings.PlanetSafetyRadius.Evaluate(masses[i]);
		}
		
		for (var i = 0; i < count; i++)
		{
			// Only instantiate children above the mass floor
			if (masses[i] > Settings.MassFloor)
			{
				var child = new GeneratorPlanet
				{
					Settings = Settings,
					Parent = this,
					Mass = masses[i],
					Distance = distances[i],
					Phase = random.NextFloat()
				};
				//child.Period = Context.GlobalData.OrbitalPeriod(child.Distance);
				child.ChildDistanceMinimum = Settings.PlanetSafetyRadius.Evaluate(child.Mass) * 2;
				// Maximum child distance of child is the smallest distance to either of its neighbors
				child.ChildDistanceMaximum = min(i == 0 ? child.Distance - ChildDistanceMinimum : child.Distance - distances[i - 1],
										 i < count - 1 ? distances[i + 1] - child.Distance : float.PositiveInfinity);
				if (float.IsNaN(child.Distance))
					throw new NotFiniteNumberException($"Planet created with NaN distance, something went very wrong!");
				if (child.ChildDistanceMaximum > child.ChildDistanceMinimum)
					Children.Add(child);
			}
		}
	}
}