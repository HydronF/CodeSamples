#include "stdafx.h"
#include "Physics.h"
#include "CollisionBox.h"

Physics* Physics::s_thePhysics = nullptr;

Physics::AABB::AABB()
{
}

Physics::AABB::AABB(Vector3 min, Vector3 max)
	:mMinCorner(min)
	,mMaxCorner(max)
{
}

Physics::LineSegment::LineSegment()
{
}

Physics::LineSegment::LineSegment(Vector3 start, Vector3 end)
	:mStartPoint(start)
	,mEndPoint(end)
{
}

Physics::Physics()
{
	DbgAssert(s_thePhysics == nullptr, "There can only be one Physics");
	s_thePhysics = this;
}

Physics::~Physics()
{
	DbgAssert(s_thePhysics == this, "There can only be one Physics");
	s_thePhysics = nullptr;
}

bool Physics::Intersect(const AABB& a, const AABB& b, AABB* pOverlap)
{
	// Return early if they don't intersect
	if (a.mMaxCorner.x < b.mMinCorner.x) return false;
	if (a.mMaxCorner.y < b.mMinCorner.y) return false;
	if (a.mMaxCorner.z < b.mMinCorner.z) return false;
	if (b.mMaxCorner.x < a.mMinCorner.x) return false;
	if (b.mMaxCorner.y < a.mMinCorner.y) return false;
	if (b.mMaxCorner.z < a.mMinCorner.z) return false;
	
	// Now we know that they DO intersect
	AABB overlap;
	overlap.mMinCorner.x = Math::Max(a.mMinCorner.x, b.mMinCorner.x);
	overlap.mMinCorner.y = Math::Max(a.mMinCorner.y, b.mMinCorner.y);
	overlap.mMinCorner.z = Math::Max(a.mMinCorner.z, b.mMinCorner.z);
	overlap.mMaxCorner.x = Math::Min(a.mMaxCorner.x, b.mMaxCorner.x);
	overlap.mMaxCorner.y = Math::Min(a.mMaxCorner.y, b.mMaxCorner.y);
	overlap.mMaxCorner.z = Math::Min(a.mMaxCorner.z, b.mMaxCorner.z);

	if (pOverlap != nullptr) 
	{
		*pOverlap = overlap;
	}

	return true;
}

bool Physics::Intersect(const LineSegment& segment, const AABB& box, Vector3* pHitPoint)
{
	Vector3 dir = segment.mEndPoint - segment.mStartPoint;
	float length = dir.Length();
	dir.Normalize();
	float tMin = FLT_MIN;
	float tMax = FLT_MAX;
	{
		if (Math::IsCloseEnuf(dir.x, 0.0f))
		{
			// Ray is parallel to slab. No hit if origin not within slab
			if (segment.mStartPoint.x < box.mMinCorner.x || segment.mStartPoint.x > box.mMaxCorner.x)
			{
				return false;
			}
		}
		else
		{
			// Compute intersection t value of ray with near and far plane of slab
			float ood = 1.0f / dir.x;
			float t1 = (box.mMinCorner.x - segment.mStartPoint.x) * ood;
			float t2 = (box.mMaxCorner.x - segment.mStartPoint.x) * ood;
			// Make sure that t1 is near plane and t2 is far plane.
			if (t1 > t2)
			{
				std::swap(t1, t2);
			}
			// Check range
			if (t1 > length)
			{
				return false;
			}
			tMin = max(tMin, t1);
			tMax = min(tMax, t2);
			// Exit if slab intersection is empty
			if (tMin > tMax)
			{
				return false;
			}
		}
	}
	{
		if (Math::IsCloseEnuf(dir.y, 0.0f))
		{
			// Ray is parallel to slab. No hit if origin not within slab
			if (segment.mStartPoint.y < box.mMinCorner.y || segment.mStartPoint.y > box.mMaxCorner.y)
			{
				return false;
			}
		}
		else
		{
			// Compute intersection t value of ray with near and far plane of slab
			float ood = 1.0f / dir.y;
			float t1 = (box.mMinCorner.y - segment.mStartPoint.y) * ood;
			float t2 = (box.mMaxCorner.y - segment.mStartPoint.y) * ood;
			// Make sure that t1 is near plane and t2 is far plane.
			if (t1 > t2)
			{
				std::swap(t1, t2);
			}
			// Check range
			if (t1 > length)
			{
				return false;
			}
			tMin = max(tMin, t1);
			tMax = min(tMax, t2);
			// Exit if slab intersection is empty
			if (tMin > tMax)
			{
				return false;
			}
		}
	}
	{
		if (Math::IsCloseEnuf(dir.z, 0.0f))
		{
			// Ray is parallel to slab. No hit if origin not within slab
			if (segment.mStartPoint.z < box.mMinCorner.z || segment.mStartPoint.z > box.mMaxCorner.z)
			{
				return false;
			}
		}
		else
		{
			// Compute intersection t value of ray with near and far plane of slab
			float ood = 1.0f / dir.z;
			float t1 = (box.mMinCorner.z - segment.mStartPoint.z) * ood;
			float t2 = (box.mMaxCorner.z - segment.mStartPoint.z) * ood;
			// Make sure that t1 is near plane and t2 is far plane.
			if (t1 > t2)
			{
				std::swap(t1, t2);
			}
			// Check range
			if (t1 > length)
			{
				return false;
			}
			tMin = max(tMin, t1);
			tMax = min(tMax, t2);
			// Exit if slab intersection is empty
			if (tMin > tMax)
			{
				return false;
			}
		}
	}
	*pHitPoint = segment.mStartPoint + tMin * dir;
	return true;
}

bool Physics::UnitTest()
{
	struct TestAABB
	{
		AABB a;
		AABB b;
		AABB overlap;
	};

	const TestAABB testAABB[] =
	{
		{
			AABB(Vector3(0.0f, 0.0f, 0.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(0.0f, 0.0f, 0.0f), Vector3(10.0f, 10.0f, 10.0f)),
			AABB(Vector3(0.0f, 0.0f, 0.0f), Vector3(10.0f, 10.0f, 10.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-110.0f, -10.0f, -10.0f), Vector3(-90.0f, 10.0f, 10.0f)),
			AABB(Vector3(-100.0f, -10.0f, -10.0f), Vector3(-90.0f, 10.0f, 10.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(90.0f, -10.0f, -10.0f), Vector3(110.0f, 10.0f, 10.0f)),
			AABB(Vector3(90.0f, -10.0f, -10.0f), Vector3(100.0f, 10.0f, 10.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, -110.0f, -10.0f), Vector3(10.0f, -90.0f, 10.0f)),
			AABB(Vector3(-10.0f, -100.0f, -10.0f), Vector3(10.0f, -90.0f, 10.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, 90.0f, -10.0f), Vector3(10.0f, 110.0f, 10.0f)),
			AABB(Vector3(-10.0f, 90.0f, -10.0f), Vector3(10.0f, 100.0f, 10.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, -10.0f, -110.0f), Vector3(10.0f, 10.0f, -90.0f)),
			AABB(Vector3(-10.0f, -10.0f, -100.0f), Vector3(10.0f, 10.0f, -90.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, -10.0f, 90.0f), Vector3(10.0f, 10.0f, 110.0f)),
			AABB(Vector3(-10.0f, -10.0f, 90.0f), Vector3(10.0f, 10.0f, 100.0f))
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-120.0f, -10.0f, -10.0f), Vector3(-110.0f, 10.0f, 10.0f)),
			AABB(Vector3::One, Vector3::Zero)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(110.0f, -10.0f, -10.0f), Vector3(120.0f, 10.0f, 10.0f)),
			AABB(Vector3::One, Vector3::Zero)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, -120.0f, -10.0f), Vector3(10.0f, -110.0f, 10.0f)),
			AABB(Vector3::One, Vector3::Zero)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, 110.0f, -10.0f), Vector3(10.0f, 120.0f, 10.0f)),
			AABB(Vector3::One, Vector3::Zero)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, -10.0f, -120.0f), Vector3(10.0f, 10.0f, -110.0f)),
			AABB(Vector3::One, Vector3::Zero)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			AABB(Vector3(-10.0f, -10.0f, 110.0f), Vector3(10.0f, 10.0f, 120.0f)),
			AABB(Vector3::One, Vector3::Zero)
		},
	};

	struct TestSegment
	{
		AABB box;
		LineSegment segment;
		bool hit;
		Vector3 point;
	};
	const TestSegment testSegment[] = 
	{
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(-110.0f, 0.0f, 0.0f), Vector3(-90.0f, 0.0f, 0.0f)), 
			true, Vector3(-100.0f, 0.0f, 0.0f)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(0.0f, -110.0f, 0.0f), Vector3(0.0f, -90.0f, 0.0f)), 
			true, Vector3(0.0f, -100.0f, 0.0f)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(0.0f, 0.0f, -110.0f), Vector3(0.0f, 0.0f, -90.0f)),
			true, Vector3(0.0f, 0.0f, -100.0f)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(110.0f, 0.0f, 0.0f), Vector3(90.0f, 0.0f, 0.0f)),
			true, Vector3(100.0f, 0.0f, 0.0f)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(0.0f, 110.0f, 0.0f), Vector3(0.0f, 90.0f, 0.0f)), 
			true, Vector3(0.0f, 100.0f, 0.0f)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)), 
			LineSegment(Vector3(0.0f, 0.0f, 110.0f), Vector3(0.0f, 0.0f, 90.0f)), 
			true, Vector3(0.0f, 0.0f, 100.0f)
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(-120.0f, 0.0f, 0.0f), Vector3(-110.0f, 0.0f, 0.0f)), 
			false, Vector3::Zero
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(0.0f, -120.0f, 0.0f), Vector3(0.0f, -110.0f, 0.0f)), 
			false, Vector3::Zero
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(0.0f, 0.0f, -120.0f), Vector3(0.0f, 0.0f, -110.0f)), 
			false, Vector3::Zero
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(120.0f, 0.0f, 0.0f), Vector3(110.0f, 0.0f, 0.0f)), 
			false, Vector3::Zero
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)), 
			LineSegment(Vector3(0.0f, 120.0f, 0.0f), Vector3(0.0f, 110.0f, 0.0f)), 
			false, Vector3::Zero
		},
		{
			AABB(Vector3(-100.0f, -100.0f, -100.0f), Vector3(100.0f, 100.0f, 100.0f)),
			LineSegment(Vector3(0.0f, 0.0f, 120.0f), Vector3(0.0f, 0.0f, 110.0f)),
			false,  Vector3::Zero
		},
	};

	for (size_t i = 0; i < 7; ++i)
	{
		AABB overlap;
		if (!Intersect(testAABB[i].a, testAABB[i].b, &overlap))
		{
			return false;
		}
		else if (!Vector3::IsCloseEnuf(overlap.mMinCorner, testAABB[i].overlap.mMinCorner) ||
				 !Vector3::IsCloseEnuf(overlap.mMaxCorner, testAABB[i].overlap.mMaxCorner))
		{
			return false;
		}
	}
	for (size_t i = 7; i < 13; ++i)
	{
		if (Intersect(testAABB[i].a, testAABB[i].b))
		{
			return false;
		}
	}

	for (size_t i = 0; i < 12; i++)
	{
		Vector3 intersection;
		if (Intersect(testSegment[i].segment, testSegment[i].box, &intersection) != testSegment[i].hit) 
		{
			return false;
		}
		if (!Vector3::IsCloseEnuf(intersection, testSegment[i].point))
		{
			return false;
		}
	}

	return true;
}

bool Physics::RayCast(const LineSegment& segment, Vector3* pHitPoint)
{
	bool intersecting = false;
	float minLenSq = FLT_MAX;
	Vector3 outHitPoint;
	for (const CollisionBox* box : mColBox)
	{
		Vector3 hitPoint;
		if (Intersect(segment, box->GetAABB(), &hitPoint))
		{
			if ((hitPoint - segment.mStartPoint).LengthSq() < minLenSq)
			{
				intersecting = true;
				minLenSq = (hitPoint - segment.mStartPoint).LengthSq();
				outHitPoint = hitPoint;
			}
		}
	}
	if (intersecting)
	{
		*pHitPoint = outHitPoint;
		return true;
	}
	else {
		return false;
	}
}

void Physics::AddObj(const CollisionBox* pObj)
{
	mColBox.push_back(pObj);
}

void Physics::RemoveObj(const CollisionBox* pObj)
{
	mColBox.erase(std::remove(mColBox.begin(), mColBox.end(), pObj), mColBox.end());
}


