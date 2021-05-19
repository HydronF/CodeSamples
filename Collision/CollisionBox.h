#pragma once
#include "Physics.h"
#include "Component.h"

class CollisionBox : public Component
{
public:
	CollisionBox(RenderObj* pObj);
	~CollisionBox();

	void LoadProperties(const rapidjson::Value& properties) override;
	Physics::AABB GetAABB() const;

private:
	Physics::AABB box;
};

