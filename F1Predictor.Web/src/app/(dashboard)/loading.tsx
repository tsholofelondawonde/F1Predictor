import { Card } from "@/shared/components/Card";
import { TableSkeleton } from "@/shared/components/Skeleton";

export default function DashboardLoading() {
  return (
    <Card>
      <TableSkeleton rows={8} />
    </Card>
  );
}
