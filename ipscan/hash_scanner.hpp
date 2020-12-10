#pragma once

#include "hash_file.hpp"

#include <array>
#include <vector>

namespace psi {

class hash_scanner
{
	using hash_data = std::array<uint8_t, 16>;

public:
	void source_add(std::string const& filename);
	void source_clear();

	void test(std::vector<std::array<uint8_t, 16>> const& input,
		std::vector<std::array<uint8_t, 16>>& output);

private:
	void read(hash_file const& source, std::size_t elements);

	bool find(std::array<uint8_t, 16> const& entry) const;

private:
	std::vector<hash_file> sources_;
	std::vector<hash_data> data_;
};

} // namespace psi