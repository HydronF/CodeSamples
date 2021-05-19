#include "stdafx.h"
#include "CollisionBox.h"
#include "RenderObj.h"
#include "Physics.h"

CollisionBox::CollisionBox(RenderObj* pObj)
	:Component(pObj)
{
	Physics::Get()->AddObj(this);
}

CollisionBox::~CollisionBox()
{
	Physics::Get()->RemoveObj(this);
}

void CollisionBox::LoadProperties(const rapidjson::Value& properties)
{
	GetVectorFromJSON(properties, "min", box.mMinCorner);
	GetVectorFromJSON(properties, "max", box.mMaxCorner);
}

Physics::AABB CollisionBox::GetAABB() const
{
	Vector3 center = (box.mMinCorner + box.mMaxCorner) * 0.5f;
	// Apply scale and translation
	Vector3 min = center + (box.mMinCorner - center) * mRenderObj->GetScale() + mRenderObj->GetPosition();
	Vector3 max = center + (box.mMaxCorner - center) * mRenderObj->GetScale() + mRenderObj->GetPosition();
	return Physics::AABB(min, max);
}
