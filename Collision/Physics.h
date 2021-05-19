#pragma once

#include "engineMath.h"

class Physics
{
public:
	Physics();
	~Physics();
	static Physics* Get() { return s_thePhysics; }

	class AABB
	{
	public:
		AABB();
		AABB(Vector3 min, Vector3 max);

		Vector3 mMinCorner;
		Vector3 mMaxCorner;
	};

	class LineSegment
	{
	public:
		LineSegment();
		LineSegment(Vector3 start, Vector3 end);
		Vector3 mStartPoint;
		Vector3 mEndPoint;
	};

	static bool Intersect(const AABB& a, const AABB& b, AABB* pOverlap = nullptr); 
	static bool Intersect(const LineSegment& segment, const AABB& box, Vector3* pHitPoint = nullptr); 
	static bool UnitTest();
	
	bool RayCast(const LineSegment& segment, Vector3* pHitPoint = nullptr);

	void AddObj(const class CollisionBox* pObj);
	void RemoveObj(const class CollisionBox* pObj);

private:
	static Physics* s_thePhysics;
	std::vector<const class CollisionBox*> mColBox;
};

