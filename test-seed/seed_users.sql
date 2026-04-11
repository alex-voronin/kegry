-- SQL Script to seed 40 users into the user_profile.users table
-- Expected Schema: user_profile
-- Table Name: users

INSERT INTO user_profile.users (
    id, 
    first_name, 
    last_name, 
    bio, 
    avatar_image_s3key, 
    cover_image_s3key, 
    birthday, 
    gender, 
    is_public, 
    is_deleted, 
    created_at,
    updated_at
) VALUES
(gen_random_uuid(), 'Alex', 'Johnson', 'Loves coffee and morning runs.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1992-05-15', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Emma', 'Smith', 'Passionate about digital art and UI design.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1995-08-22', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Liam', 'Wilson', 'Exploring the world through photography.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1988-03-10', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Sophia', 'Brown', 'Bookworm and aspiring novelist.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1994-11-28', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Noah', 'Davis', 'Gym enthusiast and fitness coach.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1990-07-04', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Olivia', 'Miller', 'Nature lover and weekend hiker.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1993-01-12', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Ethan', 'Moore', 'Tech geek and software developer.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1996-09-15', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Ava', 'Taylor', 'Foodie always looking for the best tacos.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1991-04-30', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'James', 'Anderson', 'Motorcycle repair enthusiast.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1985-12-05', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Isabella', 'Thomas', 'Yoga practitioner and meditation fan.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1997-06-18', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Lucas', 'Jackson', 'Vinyl records collector.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1989-10-25', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Mia', 'White', 'Aspiring chef specializing in pastry.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1994-02-14', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'William', 'Harris', 'Amateur astronomers and star gazer.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1987-11-11', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Charlotte', 'Martin', 'Loves classic movies and popcorn.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1991-03-31', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Benjamin', 'Thompson', 'Always up for a road trip.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1990-09-09', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Amelia', 'Garcia', 'Lover of independent theatre.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1995-12-25', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Michael', 'Martinez', 'Board game evening enthusiast.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1986-05-20', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Harper', 'Robinson', 'Fluent in three languages and learning more.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1998-02-02', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Alexander', 'Clark', 'Amateur woodworker and DIY guy.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1984-08-16', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Evelyn', 'Rodriguez', 'Gardening and sustainable living advocate.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1992-10-30', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Daniel', 'Lewis', 'Jazz music and late night cafes.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1990-06-15', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Abigail', 'Lee', 'Always drawing on napkins.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1996-03-22', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Matthew', 'Walker', 'Loves mountain biking and fresh air.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1988-12-01', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Emily', 'Hall', 'Dog lover and volunteer at the shelter.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1993-07-25', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Henry', 'Allen', 'Historical documentaries buff.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1985-04-18', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Ella', 'Young', 'Fashion design and vintage clothes.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1997-11-05', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Sebastian', 'Hernandez', 'Salsa dancing and tropical music.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1991-08-14', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Scarlett', 'King', 'Podcasts and long walks.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1994-05-29', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Jackson', 'Wright', 'Surfing enthusiast from the coast.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1989-02-12', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Victoria', 'Lopez', 'Coffee art hobbyist.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1992-12-14', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Samuel', 'Hill', 'Chess player and puzzle solver.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1987-06-03', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Madison', 'Scott', 'Avid traveler and amateur vlogger.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1996-10-18', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'David', 'Green', 'Backpacking and minimalist lifestyle.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1983-04-09', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Luna', 'Adams', 'Night sky photography.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1998-09-21', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Joseph', 'Baker', 'Cooking Italian food from scratch.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1986-11-30', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Grace', 'Nelson', 'Knitting sweaters for everyone.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1990-05-14', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Carter', 'Carter', 'Basketball on weekends.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1994-08-08', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Layla', 'Mitchell', 'Singing in a local choir.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1995-01-19', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Owen', 'Perez', 'Aquarium maintenance hobby.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1988-07-27', 0, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(gen_random_uuid(), 'Chloe', 'Roberts', 'Baking the perfect loaf of bread.', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg', 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/cover.jpg', '1993-02-28', 1, true, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Set location for the newly inserted users in Belgrade area
INSERT INTO location.locations (user_id, coordinates, timestamp)
SELECT id, ST_SetSRID(ST_MakePoint(20.4489 + (random() - 0.5) * 0.02, 44.7866 + (random() - 0.5) * 0.02), 4326), CURRENT_TIMESTAMP
FROM user_profile.users
WHERE avatar_image_s3key = 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg'
ON CONFLICT (user_id) DO UPDATE SET 
    coordinates = EXCLUDED.coordinates,
    timestamp = EXCLUDED.timestamp;

-- Assign 3 random hobbies to each of the newly seeded users
INSERT INTO user_profile.user_hobby_link (users_id, hobbies_id)
SELECT u.id, h.id
FROM (
    SELECT id 
    FROM user_profile.users 
    WHERE avatar_image_s3key = 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg'
) u
CROSS JOIN LATERAL (
    SELECT id 
    FROM user_profile.hobbies 
    ORDER BY random() 
    LIMIT 3
) h
ON CONFLICT DO NOTHING;

-- FeedUserRelation seeds: 20 random relations for each user (none in meeting)
INSERT INTO feed.user_relations (
    id, 
    my_user_id, 
    other_user_id, 
    rating, 
    is_blocked, 
    report_message, 
    is_friend, 
    is_in_meeting, 
    should_rate, 
    invite_expires_at, 
    invited_at, 
    skipped_at, 
    last_seen_at
)
SELECT 
    gen_random_uuid(), 
    u1.id, 
    u2.id, 
    NULL, 
    false, 
    NULL, 
    false, 
    false, 
    false, 
    CURRENT_TIMESTAMP + interval '1 day', 
    CURRENT_TIMESTAMP, 
    '0001-01-01 00:00:00+00', 
    CURRENT_TIMESTAMP
FROM (
    SELECT id 
    FROM user_profile.users 
    WHERE avatar_image_s3key = 'user_4117e9874570268472469b977e0b88296d8d9c3f80381a5ca85fc3a8f68f109e/avatar.jpg'
) u1
CROSS JOIN LATERAL (
    SELECT id 
    FROM user_profile.users 
    WHERE id <> u1.id 
    ORDER BY random() 
    LIMIT 20
) u2
ON CONFLICT DO NOTHING;

-- Reset seen_npc_at for all users
update engagement.users set seen_npc_at = '2020-01-01 01:01:01';