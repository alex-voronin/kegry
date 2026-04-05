-- Seed 40 Companies and 40 Verified Venues in Belgrade area
DO $$
DECLARE
    user_record RECORD;
    company_id UUID;
    venue_id UUID;
    venue_count INT := 1;
    venue_names TEXT[] := ARRAY[
        'The Rusty Anchor', 'Neon Nights', 'Crystal Lounge', 'Silver Spoon', 
        'Golden Gate', 'Emerald Isle', 'Velvet Room', 'Skyline Bar', 
        'The Hideout', 'Urban Oasis', 'Retro Rail', 'Modern Muse', 
        'Vintage Vine', 'Blue Bayou', 'Red River', 'Green Garden', 
        'White Willow', 'Black Bear', 'Gray Goose', 'Purple Pear', 
        'Orange Orchid', 'Yellow Yard', 'Pink Panther', 'Brown Bear', 
        'Crimson Cafe', 'Azure Attic', 'Amber Arch', 'Indigo Inn', 
        'Teal Terrace', 'Olive Orchard', 'Coral Cove', 'Salmon Shack', 
        'Peach Palace', 'Mint Muse', 'Lavender Loft', 'Ivory Island', 
        'Ebony Edge', 'Copper Cup', 'Bronze Bar', 'Gold Grill'
    ];
BEGIN
    FOR user_record IN (
        SELECT id 
        FROM user_profile.users 
        WHERE avatar_image_s3key = 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg'
        ORDER BY id
        LIMIT 40
    ) LOOP
        company_id := gen_random_uuid();
        venue_id := gen_random_uuid();
        
        -- Insert Company
        INSERT INTO venue.companies (id, name, owner_id, email, avatar_image_s3key, is_deleted, created_at, updated_at)
        VALUES (
            company_id, 
            venue_names[venue_count] || ' Co.', 
            user_record.id, 
            'contact@' || lower(replace(venue_names[venue_count], ' ', '')) || '.com',
            'venue_covers/company_avatar_' || venue_count || '.jpg',
            false,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );
        
        -- Insert Default Loyalty Levels (matching Company.SetDefaultLoyaltyLevels)
        INSERT INTO venue.loyalty_levels (id, company_id, visits_upper_bound, is_active)
        VALUES 
            (0, company_id, 3, true),
            (1, company_id, 10, true),
            (2, company_id, 15, true),
            (3, company_id, 20, true);
            
        -- Insert Venue
        INSERT INTO venue.venues (
            id, qr_code, company_id, cover_image_s3key, address, website_url, 
            google_maps_url, instagram_url, phone_number, working_hours, 
            coordinates, time_zone_id, is_verified, is_deleted, created_at, updated_at
        )
        VALUES (
            venue_id,
            gen_random_uuid(),
            company_id,
            'venue_covers/venue_cover_' || venue_count || '.jpg',
            venue_count || ' Main St, Belgrade',
            'https://' || lower(replace(venue_names[venue_count], ' ', '')) || '.com',
            'https://maps.google.com/?q=' || (44.7866 + (random() - 0.5) * 0.05) || ',' || (20.4489 + (random() - 0.5) * 0.05),
            'https://instagram.com/' || lower(replace(venue_names[venue_count], ' ', '')),
            '+381 11 123456' || venue_count,
            'Mon-Sun: 08:00 - 00:00',
            ST_SetSRID(ST_MakePoint(20.4489 + (random() - 0.5) * 0.05, 44.7866 + (random() - 0.5) * 0.05), 4326),
            'Europe/Belgrade',
            true,
            false,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );
        
        venue_count := venue_count + 1;
    END LOOP;
END $$;

-- Seed 2 discounts for each of the seeded venues
DO $$
DECLARE
    venue_record RECORD;
    discount_0_id UUID;
    discount_1_id UUID;
BEGIN
    FOR venue_record IN (
        SELECT v.id, v.company_id 
        FROM venue.venues v
        JOIN venue.companies c ON v.company_id = c.id
        WHERE c.avatar_image_s3key LIKE 'venue_covers/company_avatar_%'
        ORDER BY v.created_at DESC
    ) LOOP
        discount_0_id := gen_random_uuid();
        discount_1_id := gen_random_uuid();
        
        -- Discount for Loyalty Level 0 (5%)
        INSERT INTO venue.discounts (id, venue_id, company_id, loyalty_level_id, percent, created_at, updated_at)
        VALUES (discount_0_id, venue_record.id, venue_record.company_id, 0, 5, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        
        -- Monday to Friday (1-5)
        INSERT INTO venue.discount_days (discount_id, day_of_week) VALUES
            (discount_0_id, 1), (discount_0_id, 2), (discount_0_id, 3), (discount_0_id, 4), (discount_0_id, 5), (discount_0_id, 6), (discount_0_id, 0);
            
        -- Discount for Loyalty Level 1 (10%)
        INSERT INTO venue.discounts (id, venue_id, company_id, loyalty_level_id, percent, created_at, updated_at)
        VALUES (discount_1_id, venue_record.id, venue_record.company_id, 1, 10, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
        
        -- All week (0-6)
        INSERT INTO venue.discount_days (discount_id, day_of_week) VALUES
            (discount_1_id, 0), (discount_1_id, 1), (discount_1_id, 2), (discount_1_id, 3), (discount_1_id, 4), (discount_1_id, 5), (discount_1_id, 6);
            
    END LOOP;
END $$;